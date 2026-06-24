using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Services;

public interface IMovimientoPuntoService
{
    Task<List<MovimientoPuntoResponseDto>> GetByUsuarioId(int usuarioId);
    Task<List<MovimientoPuntoResponseDto>> GetByUsuarioIdYTipo(int usuarioId, string tipo);
    Task<MovimientoPuntoResponseDto?>      GetById(int id);
}
