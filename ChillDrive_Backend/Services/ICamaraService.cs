using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Services;

public interface ICamaraService
{
    Task<List<CamaraResponseDto>>                         GetAll();
    Task<List<CamaraResponseDto>>                         GetByVehiculoId(int vehiculoId);
    Task<CamaraResponseDto?>                              GetById(int id);
    Task<(bool success, string mensaje, int? id)>         Registrar(CamaraRequestDto dto);
    Task<(bool success, string mensaje)>                  Actualizar(int id, CamaraRequestDto dto);
    Task<(bool success, string mensaje)>                  Eliminar(int id);
    // Endpoint específico de negocio: cambiar estado de conexión
    Task<(bool success, string mensaje)>                  CambiarEstado(int id, string nuevoEstado);
}