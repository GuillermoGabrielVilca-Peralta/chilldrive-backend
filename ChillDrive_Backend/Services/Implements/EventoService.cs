using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Mappings;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;
using ChillDrive_Backend.Repositories;

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
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje, int? id)> Registrar(EventoRequestDto dto)
    {
        // Validar tipo de evento contra los permitidos por la base
        if (!TiposValidos.Contains(dto.TipoEvento))
            return (false, $"Tipo de evento inválido. Valores permitidos: {string.Join(", ", TiposValidos)}", null);

        // Validar cámara
        var camara = await _camaraRepo.GetById(dto.CamaraId);
        if (camara is null)
            return (false, "La cámara no existe.", null);

        // Validar viaje si viene informado (los puntos solo cuentan dentro de un viaje,
        // porque es lo que nos dice de qué conductor son).
        Viaje? viaje = null;
        if (dto.ViajeId.HasValue)
        {
            viaje = await _viajeRepo.GetById(dto.ViajeId.Value);
            if (viaje is null)
                return (false, "El viaje no existe.", null);

            if (viaje.Estado != "En Curso")
                return (false, "No se pueden registrar eventos en un viaje que no está en curso.", null);
        }

        // El backend decide cuántos puntos vale el evento según su tipo (no el cliente).
        var impacto = ImpactoEventos.Para(dto.TipoEvento);
        var evento  = dto.ToEntity(impacto);

        return await _uow.ExecuteInTransactionAsync<(bool success, string mensaje, int? id)>(async () =>
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

            return (true, "Evento registrado correctamente.", id);
        });
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
