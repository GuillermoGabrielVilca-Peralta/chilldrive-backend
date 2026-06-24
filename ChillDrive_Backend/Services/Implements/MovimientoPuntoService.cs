using ChillDrive_Backend.Models.DTOs.Mappings;
using ChillDrive_Backend.Models.DTOs.Response;
using ChillDrive_Backend.Repositories;

namespace ChillDrive_Backend.Services.Implements;

public class MovimientoPuntoService : IMovimientoPuntoService
{
    private readonly IMovimientoPuntoRepository _repo;

    public MovimientoPuntoService(IMovimientoPuntoRepository repo)
    {
        _repo = repo;
    }

    // Historial completo de puntos de un usuario
    public async Task<List<MovimientoPuntoResponseDto>> GetByUsuarioId(int usuarioId)
    {
        var lista = await _repo.GetByUsuarioId(usuarioId);
        return lista.Select(m => m.ToResponse()).ToList();
    }

    // Historial filtrado por tipo de movimiento (Ganados, Canjeados, ...)
    public async Task<List<MovimientoPuntoResponseDto>> GetByUsuarioIdYTipo(int usuarioId, string tipo)
    {
        var lista = await _repo.GetByUsuarioIdYTipo(usuarioId, tipo);
        return lista.Select(m => m.ToResponse()).ToList();
    }

    // Un movimiento específico
    public async Task<MovimientoPuntoResponseDto?> GetById(int id)
    {
        var m = await _repo.GetById(id);
        return m?.ToResponse();
    }
}
