using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Services;

public interface ISuscripcionService
{
    Task<List<SuscripcionResponseDto>>                    GetAll();
    Task<List<SuscripcionResponseDto>>                    GetByUsuarioId(int usuarioId);
    Task<SuscripcionResponseDto?>                         GetById(int id);
    Task<SuscripcionResponseDto?>                         GetActivaByUsuarioId(int usuarioId);
    Task<(bool success, string mensaje, int? id)>         Registrar(SuscripcionRequestDto dto);
    Task<(bool success, string mensaje)>                  Cancelar(int id);
}