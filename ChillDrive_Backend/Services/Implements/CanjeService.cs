using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Mappings;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;
using ChillDrive_Backend.Repositories;

namespace ChillDrive_Backend.Services.Implements;

public class CanjeService : ICanjeService
{
    private readonly ICanjeRepository           _repo;
    private readonly IPremioRepository          _premioRepo;
    private readonly IPuntosUsuarioRepository   _puntosRepo;
    private readonly IUsuarioRepository         _usuarioRepo;
    private readonly IMovimientoPuntoRepository _movimientoRepo;
    private readonly IUnitOfWork                _uow;

    public CanjeService(
        ICanjeRepository           repo,
        IPremioRepository          premioRepo,
        IPuntosUsuarioRepository   puntosRepo,
        IUsuarioRepository         usuarioRepo,
        IMovimientoPuntoRepository movimientoRepo,
        IUnitOfWork                uow)
    {
        _repo           = repo;
        _premioRepo     = premioRepo;
        _puntosRepo     = puntosRepo;
        _usuarioRepo    = usuarioRepo;
        _movimientoRepo = movimientoRepo;
        _uow            = uow;
    }

    public async Task<List<CanjeResponseDto>> GetAll()
    {
        var lista = await _repo.GetAll();
        return lista.Select(c => c.ToResponse()).ToList();
    }

    public async Task<List<CanjeResponseDto>> GetByUsuarioId(int usuarioId)
    {
        var lista = await _repo.GetByUsuarioId(usuarioId);
        return lista.Select(c => c.ToResponse()).ToList();
    }

    public async Task<CanjeResponseDto?> GetById(int id)
    {
        var c = await _repo.GetById(id);
        return c?.ToResponse();
    }

    // -------------------------------------------------------------------------
    // CANJEAR — endpoint principal de negocio
    // Valida puntos, descuenta saldo, reduce stock y genera el QR
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje, int? id)> Canjear(CanjeRequestDto dto)
    {
        // Validar usuario
        var usuario = await _usuarioRepo.GetById(dto.UsuarioId);
        if (usuario is null)
            return (false, "El usuario no existe.", null);

        // Validar premio
        var premio = await _premioRepo.GetById(dto.PremioId);
        if (premio is null)
            return (false, "El premio no existe.", null);

        if (premio.Estado != "Disponible")
            return (false, $"El premio no está disponible. Estado actual: '{premio.Estado}'.", null);

        if (premio.Stock <= 0)
            return (false, "El premio está agotado.", null);

        // Validar vigencia del premio (en fecha Perú, consistente con la lista de disponibles)
        var hoy = FechaPeru.Hoy;
        if (premio.FechaFin.HasValue && premio.FechaFin < hoy)
            return (false, "El premio ha vencido.", null);

        if (premio.FechaInicio.HasValue && premio.FechaInicio > hoy)
            return (false, "El premio aún no está activo.", null);

        // Validar puntos suficientes
        var puntos = await _puntosRepo.GetByUsuarioId(dto.UsuarioId);
        if (puntos is null || puntos.PuntosActuales < premio.PuntosRequeridos)
            return (false, $"Puntos insuficientes. Disponibles: {puntos?.PuntosActuales ?? 0}, Requeridos: {premio.PuntosRequeridos}.", null);

        // Mutaciones atómicas: descontar saldo, reducir stock, crear el canje y registrar el
        // ledger se hacen dentro de una transacción. Si algo falla, no queda nada a medias.
        return await _uow.ExecuteInTransactionAsync<(bool success, string mensaje, int? id)>(async () =>
        {
            // Descontar puntos
            await _puntosRepo.DescontarPuntos(dto.UsuarioId, premio.PuntosRequeridos);

            // Reducir stock y marcar agotado si llega a 0
            premio.Stock -= 1;
            if (premio.Stock == 0)
                premio.Estado = "Agotado";
            await _premioRepo.Update(premio);

            // Crear el canje — el código QR lo genera la BD por DEFAULT
            var canje = new Canje
            {
                UsuarioId        = dto.UsuarioId,
                PremioId         = dto.PremioId,
                PuntosUtilizados = premio.PuntosRequeridos,
                Estado           = "Pendiente",
                FechaCanje       = DateTime.UtcNow
            };

            var id = await _repo.Add(canje);

            // Deja constancia del descuento en el ledger inmutable (delta negativo).
            await _movimientoRepo.Add(new MovimientosPunto
            {
                UsuarioId       = dto.UsuarioId,
                Tipo            = MovimientoTipos.Canjeado,
                Puntos          = -premio.PuntosRequeridos,   // delta negativo: resta puntos (Canjeado exige <0)
                Descripcion     = $"Canje de premio '{premio.Nombre}'",
                CanjeId         = id,
                FechaMovimiento = DateTime.UtcNow
            });

            return (true, $"Canje realizado. Código QR generado. Puntos descontados: {premio.PuntosRequeridos}.", id);
        });
    }

    // -------------------------------------------------------------------------
    // USAR QR — el aliado escanea el código en el negocio
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje)> UsarQr(string codigoQr)
    {
        var canje = await _repo.GetByCodigoQr(codigoQr);
        if (canje is null)
            return (false, "Código QR no encontrado.");

        if (canje.Estado == "Usado")
            return (false, "Este QR ya fue utilizado.");

        if (canje.Estado == "Expirado")
            return (false, "Este QR ha expirado.");

        if (canje.Estado == "Anulado")
            return (false, "Este QR fue anulado.");

        canje.Estado   = "Usado";
        canje.FechaUso = DateTime.UtcNow;
        await _repo.Update(canje);

        return (true, $"QR validado correctamente. Premio: {canje.Premio?.Nombre}.");
    }

    // -------------------------------------------------------------------------
    // ANULAR — cancela el canje y devuelve los puntos al usuario
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje)> Anular(int id)
    {
        var canje = await _repo.GetById(id);
        if (canje is null)
            return (false, "Canje no encontrado.");

        if (canje.Estado == "Usado")
            return (false, "No se puede anular un canje ya utilizado.");

        if (canje.Estado == "Anulado")
            return (false, "El canje ya está anulado.");

        // Devolución de puntos + ledger + restauración de stock + estado del canje, en una
        // sola transacción: o se aplica todo, o nada.
        return await _uow.ExecuteInTransactionAsync<(bool success, string mensaje)>(async () =>
        {
            // Devolver puntos al usuario y registrar la devolución en el ledger (delta positivo).
            await _puntosRepo.AgregarOActualizar(canje.UsuarioId, canje.PuntosUtilizados);
            await _movimientoRepo.Add(new MovimientosPunto
            {
                UsuarioId       = canje.UsuarioId,
                Tipo            = MovimientoTipos.Ajuste,   // devolución = corrección positiva
                Puntos          = canje.PuntosUtilizados,   // delta positivo: devuelve puntos
                Descripcion     = $"Devolución por anulación del canje #{canje.Id}",
                CanjeId         = canje.Id,
                FechaMovimiento = DateTime.UtcNow
            });

            // Restaurar stock del premio
            var premio = await _premioRepo.GetById(canje.PremioId);
            if (premio is not null)
            {
                premio.Stock += 1;
                if (premio.Estado == "Agotado")
                    premio.Estado = "Disponible";
                await _premioRepo.Update(premio);
            }

            canje.Estado = "Anulado";
            await _repo.Update(canje);

            return (true, $"Canje anulado. Se devolvieron {canje.PuntosUtilizados} puntos al usuario.");
        });
    }
}