using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Services;

public interface IVehiculoService
{
    Task<List<VehiculoResponseDto>>                       GetAll();
    Task<List<VehiculoResponseDto>>                       GetByUsuarioId(int usuarioId);
    Task<VehiculoResponseDto?>                            GetById(int id);
    Task<(bool success, string mensaje, int? id)>         Registrar(VehiculoRequestDto dto);
    Task<(bool success, string mensaje)>                  Actualizar(int id, VehiculoRequestDto dto);
    Task<(bool success, string mensaje)>                  Eliminar(int id);
}