using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Mappings;
using ChillDrive_Backend.Models.DTOs.Response;
using ChillDrive_Backend.Repositories;

namespace ChillDrive_Backend.Services.Implements;

public class PuntosUsuarioService : IPuntosUsuarioService
{
    private readonly IPuntosUsuarioRepository   _repo;
    private readonly IUsuarioRepository         _usuarioRepo;
    private readonly IMovimientoPuntoRepository _movimientoRepo;
    private readonly IUnitOfWork                _uow;

    public PuntosUsuarioService(
        IPuntosUsuarioRepository   repo,
        IUsuarioRepository         usuarioRepo,
        IMovimientoPuntoRepository movimientoRepo,
        IUnitOfWork                uow)
    {
        _repo           = repo;
        _usuarioRepo    = usuarioRepo;
        _movimientoRepo = movimientoRepo;
        _uow            = uow;
    }

    public async Task<PuntosUsuarioResponseDto?> GetByUsuarioId(int usuarioId)
    {
        var p = await _repo.GetByUsuarioId(usuarioId);
        return p?.ToResponse();
    }

    // Acreditar puntos manualmente (por viaje finalizado desde el controller de viaje)
    public async Task<(bool success, string mensaje)> AcreditarPuntos(int usuarioId, int puntos)
    {
        var usuario = await _usuarioRepo.GetById(usuarioId);
        if (usuario is null)
            return (false, "El usuario no existe.");

        if (puntos <= 0)
            return (false, "Los puntos a acreditar deben ser mayores a 0.");

        // Acreditar el saldo y registrar el ledger en una sola transacción.
        return await _uow.ExecuteInTransactionAsync<(bool success, string mensaje)>(async () =>
        {
            await _repo.AgregarOActualizar(usuarioId, puntos);

            // Registra la acreditación manual en el ledger inmutable (delta positivo).
            await _movimientoRepo.Add(new MovimientosPunto
            {
                UsuarioId       = usuarioId,
                Tipo            = MovimientoTipos.Ganado,
                Puntos          = puntos,
                Descripcion     = "Acreditación manual de puntos",
                FechaMovimiento = DateTime.UtcNow
            });

            return (true, $"Se acreditaron {puntos} puntos al usuario.");
        });
    }
}