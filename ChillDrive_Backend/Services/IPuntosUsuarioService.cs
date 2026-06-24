using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Services;

public interface IPuntosUsuarioService
{
    Task<PuntosUsuarioResponseDto?>       GetByUsuarioId(int usuarioId);
    Task<(bool success, string mensaje)>  AcreditarPuntos(int usuarioId, int puntos);
}
