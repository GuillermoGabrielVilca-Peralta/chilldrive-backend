using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Services;

public interface IPremioService
{
    Task<List<PremioResponseDto>>                        GetAll();
    Task<List<PremioResponseDto>>                        GetByAliadoId(int aliadoId);
    Task<List<PremioResponseDto>>                        GetDisponibles();
    Task<PaginadoDto<PremioResponseDto>>                 GetDisponiblesPaginado(int pagina, int tamano);
    Task<PremioResponseDto?>                             GetById(int id);
    Task<(bool success, string mensaje, int? id)>        Registrar(PremioRequestDto dto);
    Task<(bool success, string mensaje)>                 Actualizar(int id, PremioRequestDto dto);
    Task<(bool success, string mensaje)>                 Eliminar(int id);
    Task<(bool success, string mensaje)>                 CambiarEstado(int id, string nuevoEstado);
}