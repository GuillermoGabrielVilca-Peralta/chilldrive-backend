using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Mappings;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;
using System.Globalization;
using ChillDrive_Backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ChillDrive_Backend.Services.Implements;

public class EventoService : IEventoService
{
    private readonly IEventoRepository          _repo;
    private readonly IViajeRepository           _viajeRepo;
    private readonly ICamaraRepository          _camaraRepo;
    private readonly IPuntosUsuarioRepository   _puntosRepo;
    private readonly IMovimientoPuntoRepository _movimientoRepo;
    private readonly IUnitOfWork                _uow;

    private static readonly string[] TiposValidos =
    [
        "Peaton Detectado", "Señal Respetada", "Señal Ignorada",
        "Carril Mantenido", "Carril Invadido", "Frenado Brusco",
        "Exceso Velocidad", "Distancia Segura", "Otro",
        "Aceleración Brusca", "Giro Brusco", "Uso Celular"
    ];

    // Defensa anti-flood (Fase C.2): dos eventos del MISMO tipo en el MISMO viaje
    // dentro de esta ventana (por hora de DETECCIÓN, no de llegada) son no-físicos
    // y se descartan. 3 s queda muy por debajo del cooldown del detector (6 s), así
    // que no genera falsos positivos ni siquiera al vaciar una cola offline de golpe.
    private const double VentanaAntiFloodSegundos = 3;

    public EventoService(
        IEventoRepository          repo,
        IViajeRepository           viajeRepo,
        ICamaraRepository          camaraRepo,
        IPuntosUsuarioRepository   puntosRepo,
        IMovimientoPuntoRepository movimientoRepo,
        IUnitOfWork                uow)
    {
        _repo           = repo;
        _viajeRepo      = viajeRepo;
        _camaraRepo     = camaraRepo;
        _puntosRepo     = puntosRepo;
        _movimientoRepo = movimientoRepo;
        _uow            = uow;
    }

    public async Task<List<EventoResponseDto>> GetAll()
    {
        var lista = await _repo.GetAll();
        return lista.Select(e => e.ToResponse()).ToList();
    }

    public async Task<List<EventoResponseDto>> GetByViajeId(int viajeId)
    {
        var lista = await _repo.GetByViajeId(viajeId);
        return lista.Select(e => e.ToResponse()).ToList();
    }

    public async Task<PaginadoDto<EventoResponseDto>> GetByViajeIdPaginado(int viajeId, int pagina, int tamano)
    {
        var skip = (pagina - 1) * tamano;
        var (items, total) = await _repo.GetByViajeIdPaginado(viajeId, skip, tamano);
        return new PaginadoDto<EventoResponseDto>
        {
            Items        = items.Select(e => e.ToResponse()).ToList(),
            Total        = total,
            Pagina       = pagina,
            Tamano       = tamano,
            TotalPaginas = (int)Math.Ceiling((double)total / tamano)
        };
    }

    public async Task<List<EventoResponseDto>> GetByCamaraId(int camaraId)
    {
        var lista = await _repo.GetByCamaraId(camaraId);
        return lista.Select(e => e.ToResponse()).ToList();
    }

    public async Task<EventoResponseDto?> GetById(int id)
    {
        var e = await _repo.GetById(id);
        return e?.ToResponse();
    }

    // -------------------------------------------------------------------------
    // REGISTRAR EVENTO — detectado por la IA en tiempo real.
    // Además de guardar el evento, aplica su impacto al puntaje del conductor:
    // las buenas conductas (señal respetada, etc.) suman y las infracciones restan,
    // de forma continua durante el viaje. Todo dentro de una transacción.
    //
    // Fase C (offline robusto):
    //  - Idempotencia: si el cliente reenvía un evento ya registrado (mismo IdLocal),
    //    se devuelve éxito SIN duplicarlo ni volver a sumar/restar puntos.
    //  - Pertenencia: el viaje debe ser del usuario autenticado (no se pueden
    //    inyectar eventos en el viaje de otro conductor).
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje, int? id, bool duplicado)> Registrar(
        EventoRequestDto dto, int usuarioActualId)
    {
        // Validar tipo de evento contra los permitidos por la base
        if (!TiposValidos.Contains(dto.TipoEvento))
            return (false, $"Tipo de evento inválido. Valores permitidos: {string.Join(", ", TiposValidos)}", null, false);

        // Idempotencia (camino rápido): si ya existe un evento con este IdLocal, es un
        // reenvío de la cola offline → confirmar como éxito sin volver a procesarlo.
        var idLocal = string.IsNullOrWhiteSpace(dto.IdLocal) ? null : dto.IdLocal;
        if (idLocal is not null)
        {
            var existente = await _repo.GetByClienteEventoId(idLocal);
            if (existente is not null)
                return (true, "Evento ya estaba registrado (idempotente).", existente.Id, true);
        }

        // Validar cámara
        var camara = await _camaraRepo.GetById(dto.CamaraId);
        if (camara is null)
            return (false, "La cámara no existe.", null, false);

        // Validar viaje si viene informado (los puntos solo cuentan dentro de un viaje,
        // porque es lo que nos dice de qué conductor son).
        Viaje? viaje = null;
        if (dto.ViajeId.HasValue)
        {
            viaje = await _viajeRepo.GetById(dto.ViajeId.Value);
            if (viaje is null)
                return (false, "El viaje no existe.", null, false);

            // Pertenencia: el viaje debe ser del usuario autenticado.
            if (viaje.UsuarioId != usuarioActualId)
                return (false, "El viaje no pertenece al usuario autenticado.", null, false);

            if (viaje.Estado != "En Curso")
                return (false, "No se pueden registrar eventos en un viaje que no está en curso.", null, false);
        }

        // Hora real del hecho: la de detección del cliente si es válida, si no la actual.
        var fechaEvento = ResolverFechaDeteccion(dto.FechaDeteccion);

        // Defensa anti-flood: descartar un evento del mismo tipo demasiado seguido en el
        // mismo viaje (posible cliente manipulado generando eventos falsos). Se evalúa por
        // hora de detección, así que una cola offline drenada de golpe no se ve afectada.
        if (viaje is not null)
        {
            var margen = TimeSpan.FromSeconds(VentanaAntiFloodSegundos);
            var hayCercano = await _repo.ExisteEventoTipoEnVentana(
                viaje.Id, dto.TipoEvento, fechaEvento - margen, fechaEvento + margen);
            if (hayCercano)
                return (false, $"Evento '{dto.TipoEvento}' descartado: otro del mismo tipo ya se registró en los últimos {VentanaAntiFloodSegundos:0} s (posible duplicado o manipulación).", null, false);
        }

        // El backend decide cuántos puntos vale el evento según su tipo (no el cliente).
        var impacto = ImpactoEventos.Para(dto.TipoEvento);
        var evento  = dto.ToEntity(impacto, fechaEvento);

        try
        {
            return await _uow.ExecuteInTransactionAsync<(bool success, string mensaje, int? id, bool duplicado)>(async () =>
            {
                var id = await _repo.Add(evento);

                // Si el evento ocurre en un viaje y tiene impacto, ajusta el saldo del conductor
                // en tiempo real y deja constancia en el ledger inmutable.
                if (viaje is not null && impacto != 0)
                {
                    await _puntosRepo.AplicarImpactoEvento(viaje.UsuarioId, impacto);
                    await _movimientoRepo.Add(new MovimientosPunto
                    {
                        UsuarioId       = viaje.UsuarioId,
                        // Ganado exige puntos>0; para las infracciones (negativas) se usa Ajuste.
                        Tipo            = impacto > 0 ? MovimientoTipos.Ganado : MovimientoTipos.Ajuste,
                        Puntos          = impacto,   // con signo: positivo suma, negativo resta
                        Descripcion     = $"Evento '{dto.TipoEvento}' en viaje #{viaje.Id}",
                        ViajeId         = viaje.Id,
                        FechaMovimiento = DateTime.UtcNow
                    });
                }

                return (true, "Evento registrado correctamente.", id, false);
            });
        }
        catch (Exception ex) when (idLocal is not null && EsViolacionDeUnico(ex))
        {
            // Carrera: dos reenvíos del mismo evento llegaron casi a la vez y ambos
            // pasaron el chequeo de idempotencia. El índice único de la BD rechazó el
            // segundo (23505); lo tratamos como duplicado idempotente, no como error.
            var existente = await _repo.GetByClienteEventoId(idLocal);
            return (true, "Evento ya estaba registrado (idempotente).", existente?.Id, true);
        }
    }

    // Detecta la violación de restricción única de PostgreSQL (SQLSTATE 23505),
    // que EF Core envuelve en DbUpdateException → PostgresException.
    private static bool EsViolacionDeUnico(Exception ex) =>
        ex is DbUpdateException { InnerException: PostgresException { SqlState: "23505" } };

    // Resuelve la hora del evento: usa la de detección del cliente (ISO-8601 UTC) si es
    // parseable y razonable (ni en el futuro ni demasiado vieja); si no, la hora actual.
    // Forzamos UTC (AssumeUniversal) para no depender de la zona horaria del request.
    private static DateTime ResolverFechaDeteccion(string? iso)
    {
        if (!string.IsNullOrWhiteSpace(iso) &&
            DateTime.TryParse(iso, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var fecha))
        {
            var ahora = DateTime.UtcNow;
            // Tolera ~5 min de adelanto (desfase de reloj) y hasta 60 días de cola offline.
            if (fecha <= ahora.AddMinutes(5) && fecha >= ahora.AddDays(-60))
                return DateTime.SpecifyKind(fecha, DateTimeKind.Utc);
        }
        return DateTime.UtcNow;
    }

    // -------------------------------------------------------------------------
    // ELIMINAR
    // -------------------------------------------------------------------------
    
    public async Task<(bool success, string mensaje)> Eliminar(int id)
    {
        var evento = await _repo.GetById(id);
        if (evento is null)
            return (false, "Evento no encontrado.");

        await _repo.Delete(evento);
        return (true, "Evento eliminado correctamente.");
    }

    // -------------------------------------------------------------------------
    // ESTADÍSTICAS POR CATEGORÍA — score 0-100 por hábito de conducción.
    // Fórmula: score = max(0, 100 + suma_de_impactos_negativos_de_esa_categoria).
    // Ejemplo: 3 frenazos × (-10) → score = max(0, 100-30) = 70.
    // Si no hay eventos de ese tipo: score = 100 (conducción perfecta en esa categoría).
    // -------------------------------------------------------------------------
    public async Task<EstadisticasConduccionDto> GetEstadisticasUsuario(int usuarioId)
    {
        var datos = await _repo.GetImpactoNegativoPorTipoParaUsuario(usuarioId);

        int ImpactoTipo(string tipo) =>
            datos.FirstOrDefault(d => d.Tipo == tipo).TotalImpacto;

        int CantidadTipo(string tipo) =>
            datos.FirstOrDefault(d => d.Tipo == tipo).Cantidad;

        int Score(string tipo) => Math.Max(0, 100 + ImpactoTipo(tipo));

        var categorias = new List<ScoreCategoriaDto>
        {
            new() { Nombre = "Velocidad",    Score = Score("Exceso Velocidad"),   Cantidad = CantidadTipo("Exceso Velocidad")   },
            new() { Nombre = "Frenado",      Score = Score("Frenado Brusco"),     Cantidad = CantidadTipo("Frenado Brusco")     },
            new() { Nombre = "Aceleración",  Score = Score("Aceleración Brusca"), Cantidad = CantidadTipo("Aceleración Brusca") },
            new() { Nombre = "Giros",        Score = Score("Giro Brusco"),        Cantidad = CantidadTipo("Giro Brusco")        },
            new() { Nombre = "Celular",      Score = Score("Uso Celular"),        Cantidad = CantidadTipo("Uso Celular")        },
        };

        var scoreGeneral = categorias.Count > 0
            ? (int)categorias.Average(c => c.Score)
            : 100;

        return new EstadisticasConduccionDto
        {
            ScoreGeneral           = scoreGeneral,
            TotalEventosNegativos  = datos.Sum(d => d.Cantidad),
            Categorias             = categorias
        };
    }
}
