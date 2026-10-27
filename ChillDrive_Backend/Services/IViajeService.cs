using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Services;

public interface IViajeService
{
    Task<List<ViajeResponseDto>>                         GetAll();
    Task<List<ViajeResponseDto>>                         GetByUsuarioId(int usuarioId);
    Task<PaginadoDto<ViajeResponseDto>>                  GetByUsuarioIdPaginado(int usuarioId, int pagina, int tamano);
    Task<List<ViajeResponseDto>>                         GetByVehiculoId(int vehiculoId);
    Task<ViajeResponseDto?>                              GetById(int id);
    Task<(bool success, string mensaje, int? id)>        Iniciar(ViajeRequestDto dto);
    Task<(bool success, string mensaje)>                 Finalizar(int id, FinalizarViajeRequestDto dto);
    Task<(bool success, string mensaje)>                 Cancelar(int id);
    Task<(bool success, string mensaje)>                 ActualizarUbicacion(int id, UbicacionViajeRequestDto dto);
    Task<(bool success, string mensaje)>                 Eliminar(int id);
}