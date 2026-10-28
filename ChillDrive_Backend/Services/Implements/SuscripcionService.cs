using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Mappings;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;
using ChillDrive_Backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ChillDrive_Backend.Services.Implements;

// Suscripciones al plan Profesional.
// - Prueba gratis y pago con puntos: se activan al instante (no hay dinero que verificar).
// - Yape / efectivo: nacen "Pendiente" y SOLO un administrador las aprueba tras verificar el pago.
// - Las fechas usan el periodo [FechaInicio, FechaFin): vigente mientras hoy < FechaFin (día Perú).
// - Una renovación se encadena: empieza cuando termina la vigente, sin perder días pagados.
public class SuscripcionService : ISuscripcionService
{
    private readonly ISuscripcionRepository     _repo;
    private readonly IUsuarioRepository         _usuarioRepo;
    private readonly IPuntosUsuarioRepository   _puntosRepo;
    private readonly IMovimientoPuntoRepository _movimientoRepo;
    private readonly IUnitOfWork                _uow;
    private readonly IConfiguration             _config;

    // Número de operación de Yape: dígitos/letras, sin espacios.
    private static readonly Regex FormatoOperacion = new("^[A-Za-z0-9-]{4,30}$", RegexOptions.Compiled);

    // Sin caracteres que se confunden al dictarlos (0/O, 1/I).
    private const string AlfabetoCodigo = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public SuscripcionService(
        ISuscripcionRepository     repo,
        IUsuarioRepository         usuarioRepo,
        IPuntosUsuarioRepository   puntosRepo,
        IMovimientoPuntoRepository movimientoRepo,
        IUnitOfWork                uow,
        IConfiguration             config)
    {
        _repo           = repo;
        _usuarioRepo    = usuarioRepo;
        _puntosRepo     = puntosRepo;
        _movimientoRepo = movimientoRepo;
        _uow            = uow;
        _config         = config;
    }

    // -------------------------------------------------------------------------
    // CONSULTAS
    // -------------------------------------------------------------------------
    public async Task<List<SuscripcionResponseDto>> GetAll()
    {
        await VencerSuscripciones();
        var lista = await _repo.GetAll();
        return lista.Select(s => s.ToResponse()).ToList();
    }

    public async Task<List<SuscripcionResponseDto>> GetByUsuarioId(int usuarioId)
    {
        var lista = await _repo.GetByUsuarioId(usuarioId);
        return lista.Select(s => s.ToResponse()).ToList();
    }

    public async Task<SuscripcionResponseDto?> GetById(int id)
    {
        var s = await _repo.GetById(id);
        return s?.ToResponse();
    }

    public async Task<SuscripcionResponseDto?> GetActivaByUsuarioId(int usuarioId)
    {
        var s = await _repo.GetActivaByUsuarioId(usuarioId);
        return s?.ToResponse();
    }

    public async Task<List<SuscripcionResponseDto>> GetPendientes()
    {
        var lista = await _repo.GetPendientes();
        return lista.Select(s => s.ToResponse()).ToList();
    }

    // Catálogo con la disponibilidad de cada plan para ESTE conductor y los datos para pagar.
    public async Task<PlanesSuscripcionResponseDto> GetPlanes(int usuarioId)
    {
        var hoy            = FechaPeru.Hoy;
        var usoPrueba      = await _repo.UsoPrueba(usuarioId);
        var tieneVigente   = await _repo.TieneVigente(usuarioId, hoy);
        var puntosActuales = (await _puntosRepo.GetByUsuarioId(usuarioId))?.PuntosActuales ?? 0;

        var planes = PlanesSuscripcion.Todos.Select(p => p.Codigo switch
        {
            PlanesSuscripcion.Prueba => p.ToResponse(
                !usoPrueba && !tieneVigente,
                usoPrueba ? "Ya usaste tu prueba gratis." : "Ya tienes el plan Profesional activo."),
            PlanesSuscripcion.Puntos => p.ToResponse(
                puntosActuales >= PlanesSuscripcion.PuntosPorMes,
                $"Te faltan {PlanesSuscripcion.PuntosPorMes - puntosActuales} puntos."),
            _ => p.ToResponse(true, null)
        }).ToList();

        return new PlanesSuscripcionResponseDto
        {
            Planes         = planes,
            YapeNumero     = _config["Pagos:YapeNumero"],
            YapeTitular    = _config["Pagos:YapeTitular"],
            PuntosPorMes   = PlanesSuscripcion.PuntosPorMes,
            PuntosActuales = puntosActuales
        };
    }

    // Estado para "Mi plan" y los avisos de vencimiento. Primero vence lo que corresponda,
    // para que el plan mostrado nunca esté desactualizado.
    public async Task<EstadoSuscripcionResponseDto?> GetEstado(int usuarioId)
    {
        await VencerSuscripciones();

        var usuario = await _usuarioRepo.GetById(usuarioId);
        if (usuario is null)
            return null;

        var hoy   = FechaPeru.Hoy;
        var lista = await _repo.GetByUsuarioId(usuarioId);   // más recientes primero

        // Con renovaciones encadenadas, la de fin más lejano marca el vencimiento real.
        var activa = lista
            .Where(s => s.Estado == PlanesSuscripcion.Activa && s.FechaFin > hoy)
            .MaxBy(s => s.FechaFin);
        int? diasRestantes = activa is null ? null : activa.FechaFin.DayNumber - hoy.DayNumber;

        var ultima = lista.FirstOrDefault();

        return new EstadoSuscripcionResponseDto
        {
            TipoPlan        = usuario.TipoPlan ?? PlanesSuscripcion.TipoGratuito,
            Activa          = activa?.ToResponse(),
            DiasRestantes   = diasRestantes,
            PorVencer       = diasRestantes <= PlanesSuscripcion.DiasAvisoVencimiento,
            Pendiente       = lista.FirstOrDefault(s => s.Estado == PlanesSuscripcion.Pendiente)?.ToResponse(),
            // Solo se muestra si es lo último que pasó (si luego pagó bien, ya no importa).
            UltimaRechazada = ultima?.Estado == PlanesSuscripcion.Rechazada ? ultima.ToResponse() : null,
            Historial       = lista.Select(s => s.ToResponse()).ToList()
        };
    }

    // -------------------------------------------------------------------------
    // SOLICITAR — el conductor elige un plan
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje, int? id)> Solicitar(int usuarioId, SolicitarSuscripcionRequestDto dto)
    {
        var plan = PlanesSuscripcion.Buscar(dto.Plan);
        if (plan is null)
            return (false, $"Plan inválido. Opciones: {string.Join(", ", PlanesSuscripcion.Todos.Select(p => p.Codigo))}.", null);

        var usuario = await _usuarioRepo.GetById(usuarioId);
        if (usuario is null)
            return (false, "El usuario no existe.", null);

        if (await _repo.GetPendienteByUsuarioId(usuarioId) is not null)
            return (false, "Ya tienes un pago en verificación. Espera a que lo revisemos o cancélalo antes de elegir otro plan.", null);

        return plan.Codigo switch
        {
            PlanesSuscripcion.Prueba => await ActivarPrueba(usuario, plan),
            PlanesSuscripcion.Puntos => await PagarConPuntos(usuario, plan),
            _                        => await RegistrarPagoConDinero(usuario, plan, dto)
        };
    }

    private async Task<(bool success, string mensaje, int? id)> ActivarPrueba(Usuario usuario, PlanesSuscripcion.Plan plan)
    {
        if (await _repo.UsoPrueba(usuario.Id))
            return (false, "Ya usaste tu prueba gratis.", null);

        if (await _repo.TieneVigente(usuario.Id, FechaPeru.Hoy))
            return (false, "Ya tienes el plan Profesional activo; la prueba gratis es para conocerlo.", null);

        var suscripcion = plan.ToEntity(usuario.Id, PlanesSuscripcion.MetodoGratis, PlanesSuscripcion.Activa, FechaPeru.Hoy);

        return await _uow.ExecuteInTransactionAsync<(bool success, string mensaje, int? id)>(async () =>
        {
            var id = await _repo.Add(suscripcion);
            await ElevarAProfesional(usuario);
            return (true, $"¡Listo! Tienes {plan.Dias} días de plan Profesional gratis.", id);
        });
    }

    private async Task<(bool success, string mensaje, int? id)> PagarConPuntos(Usuario usuario, PlanesSuscripcion.Plan plan)
    {
        var costo  = PlanesSuscripcion.PuntosPorMes;
        var puntos = await _puntosRepo.GetByUsuarioId(usuario.Id);
        if (puntos is null || puntos.PuntosActuales < costo)
            return (false, $"Puntos insuficientes. Disponibles: {puntos?.PuntosActuales ?? 0}, requeridos: {costo}.", null);

        var inicio      = await InicioDelNuevoPeriodo(usuario.Id);
        var suscripcion = plan.ToEntity(usuario.Id, PlanesSuscripcion.MetodoPuntos, PlanesSuscripcion.Activa,
                                        inicio, puntosUsados: costo);

        // Descuento, suscripción, ledger y plan: todo o nada.
        return await _uow.ExecuteInTransactionAsync<(bool success, string mensaje, int? id)>(async () =>
        {
            await _puntosRepo.DescontarPuntos(usuario.Id, costo);
            var id = await _repo.Add(suscripcion);

            await _movimientoRepo.Add(new MovimientosPunto
            {
                UsuarioId       = usuario.Id,
                Tipo            = MovimientoTipos.Canjeado,
                Puntos          = -costo,   // Canjeado exige delta negativo
                Descripcion     = $"Plan Profesional por {plan.Dias} días pagado con puntos",
                FechaMovimiento = DateTime.UtcNow
            });

            await ElevarAProfesional(usuario);
            return (true, $"¡Listo! Canjeaste {costo} puntos por {plan.Dias} días de plan Profesional.", id);
        });
    }

    private async Task<(bool success, string mensaje, int? id)> RegistrarPagoConDinero(
        Usuario usuario, PlanesSuscripcion.Plan plan, SolicitarSuscripcionRequestDto dto)
    {
        var metodo = PlanesSuscripcion.MetodosConDinero
            .FirstOrDefault(m => string.Equals(m, dto.MetodoPago?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (metodo is null)
            return (false, "Elige cómo pagarás: Yape o Efectivo.", null);

        string? referencia = null;
        string? codigo     = null;

        if (metodo == PlanesSuscripcion.Yape)
        {
            referencia = dto.NumeroOperacion?.Trim();
            if (string.IsNullOrEmpty(referencia))
                return (false, "Ingresa el número de operación que te muestra Yape al pagar.", null);
            if (!FormatoOperacion.IsMatch(referencia))
                return (false, "El número de operación no es válido: usa solo letras y números, sin espacios.", null);
            if (await _repo.ExisteReferenciaYape(referencia))
                return (false, "Ese número de operación ya fue registrado en otro pago.", null);
        }
        else
        {
            codigo = await GenerarCodigoPagoUnico();
        }

        // Las fechas son provisionales: se fijan al aprobar el pago.
        var suscripcion = plan.ToEntity(usuario.Id, metodo, PlanesSuscripcion.Pendiente, FechaPeru.Hoy,
                                        referencia, codigo);
        try
        {
            var id = await _repo.Add(suscripcion);
            var mensaje = metodo == PlanesSuscripcion.Yape
                ? "Recibimos tu pago. Lo verificaremos y activaremos tu plan en breve."
                : $"Tu código de pago es {codigo}. Preséntalo al pagar S/ {plan.PrecioSoles:0.00} en efectivo.";
            return (true, mensaje, id);
        }
        catch (Exception ex) when (EsViolacionDeUnico(ex))
        {
            // Carrera con otra solicitud: ya hay un pago pendiente o el número de operación se usó.
            return (false, "No se pudo registrar el pago: ya hay un pago en verificación o el número de operación ya se usó.", null);
        }
    }

    // -------------------------------------------------------------------------
    // APROBAR / RECHAZAR — solo administradores
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje)> Aprobar(int id)
    {
        var suscripcion = await _repo.GetById(id);
        if (suscripcion is null)
            return (false, "Suscripción no encontrada.");

        if (suscripcion.Estado != PlanesSuscripcion.Pendiente)
            return (false, $"Solo se aprueban pagos pendientes. Estado actual: '{suscripcion.Estado}'.");

        var usuario = await _usuarioRepo.GetById(suscripcion.UsuarioId);
        if (usuario is null)
            return (false, "El usuario de esta suscripción ya no existe.");

        // Si ya tiene un plan vigente, el nuevo periodo empieza cuando ese termina.
        var inicio = await InicioDelNuevoPeriodo(suscripcion.UsuarioId);

        return await _uow.ExecuteInTransactionAsync<(bool success, string mensaje)>(async () =>
        {
            suscripcion.Estado          = PlanesSuscripcion.Activa;
            suscripcion.FechaInicio     = inicio;
            suscripcion.FechaFin        = inicio.AddDays(suscripcion.Dias);
            suscripcion.FechaAprobacion = DateTime.UtcNow;
            await _repo.Update(suscripcion);

            await ElevarAProfesional(usuario);
            return (true, $"Pago aprobado. Plan Profesional activo; vence el {suscripcion.FechaFin:dd/MM/yyyy}.");
        });
    }

    public async Task<(bool success, string mensaje)> Rechazar(int id, string motivo)
    {
        var suscripcion = await _repo.GetById(id);
        if (suscripcion is null)
            return (false, "Suscripción no encontrada.");

        if (suscripcion.Estado != PlanesSuscripcion.Pendiente)
            return (false, $"Solo se rechazan pagos pendientes. Estado actual: '{suscripcion.Estado}'.");

        suscripcion.Estado        = PlanesSuscripcion.Rechazada;
        suscripcion.MotivoRechazo = motivo.Trim();
        await _repo.Update(suscripcion);
        return (true, "Pago rechazado. El conductor verá el motivo en la app.");
    }

    // -------------------------------------------------------------------------
    // CANCELAR — el conductor retira un pago que aún no se verificó
    // -------------------------------------------------------------------------
    // Un plan activo no se cancela: no hay renovación automática, así que simplemente
    // sigue vigente hasta su vencimiento (no se pierden días ya pagados).
    public async Task<(bool success, string mensaje)> Cancelar(int id)
    {
        var suscripcion = await _repo.GetById(id);
        if (suscripcion is null)
            return (false, "Suscripción no encontrada.");

        if (suscripcion.Estado == PlanesSuscripcion.Activa)
            return (false, $"Tu plan no se renueva solo: seguirá activo hasta el {suscripcion.FechaFin:dd/MM/yyyy} y luego volverás al plan Gratuito.");

        if (suscripcion.Estado != PlanesSuscripcion.Pendiente)
            return (false, $"Solo se puede cancelar un pago en verificación. Estado actual: '{suscripcion.Estado}'.");

        suscripcion.Estado = PlanesSuscripcion.Cancelada;
        await _repo.Update(suscripcion);
        return (true, "Pago cancelado. Puedes elegir otro plan cuando quieras.");
    }

    // -------------------------------------------------------------------------
    // VENCIMIENTOS
    // -------------------------------------------------------------------------
    // No hay tarea programada (Render free se duerme): se ejecuta al consultar el estado
    // del conductor y al listar en el panel, así el plan nunca se muestra desactualizado.
    public async Task<int> VencerSuscripciones()
    {
        var hoy      = FechaPeru.Hoy;
        var vencidas = await _repo.GetActivasVencidas(hoy);
        if (vencidas.Count == 0)
            return 0;

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            foreach (var s in vencidas)
            {
                s.Estado = PlanesSuscripcion.Vencida;
                await _repo.Update(s);
            }

            // Vuelve a Gratuito solo quien no tenga otra suscripción vigente (p. ej. una renovación).
            foreach (var usuarioId in vencidas.Select(s => s.UsuarioId).Distinct())
            {
                if (await _repo.TieneVigente(usuarioId, hoy))
                    continue;

                var usuario = await _usuarioRepo.GetById(usuarioId);
                if (usuario is not null && usuario.TipoPlan != PlanesSuscripcion.TipoGratuito)
                {
                    usuario.TipoPlan = PlanesSuscripcion.TipoGratuito;
                    await _usuarioRepo.Update(usuario);
                }
            }
            return true;
        });

        return vencidas.Count;
    }

    // -------------------------------------------------------------------------
    // AUXILIARES
    // -------------------------------------------------------------------------
    private async Task<DateOnly> InicioDelNuevoPeriodo(int usuarioId)
    {
        var hoy    = FechaPeru.Hoy;
        var activa = await _repo.GetActivaByUsuarioId(usuarioId);
        return activa is not null && activa.FechaFin > hoy ? activa.FechaFin : hoy;
    }

    private async Task ElevarAProfesional(Usuario usuario)
    {
        if (usuario.TipoPlan == PlanesSuscripcion.TipoProfesional)
            return;
        usuario.TipoPlan = PlanesSuscripcion.TipoProfesional;
        await _usuarioRepo.Update(usuario);
    }

    // "CD-7K3QXP": fácil de dictar o escribir al pagar en efectivo.
    private async Task<string> GenerarCodigoPagoUnico()
    {
        string codigo;
        do
        {
            var sufijo = string.Concat(Enumerable.Range(0, 6)
                .Select(_ => AlfabetoCodigo[RandomNumberGenerator.GetInt32(AlfabetoCodigo.Length)]));
            codigo = $"CD-{sufijo}";
        } while (await _repo.ExisteCodigoPago(codigo));
        return codigo;
    }

    private static bool EsViolacionDeUnico(Exception ex) =>
        ex is DbUpdateException { InnerException: PostgresException { SqlState: "23505" } };
}
