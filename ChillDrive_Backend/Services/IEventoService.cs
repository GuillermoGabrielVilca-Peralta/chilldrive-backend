using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Services;

public interface IEventoService
{
    Task<List<EventoResponseDto>>                        GetAll();
    Task<List<EventoResponseDto>>                        GetByViajeId(int viajeId);
    Task<PaginadoDto<EventoResponseDto>>                 GetByViajeIdPaginado(int viajeId, int pagina, int tamano);
    Task<List<EventoResponseDto>>                        GetByCamaraId(int camaraId);
    Task<EventoResponseDto?>                             GetById(int id);
    Task<(bool success, string mensaje, int? id)>        Registrar(EventoRequestDto dto);
    Task<(bool success, string mensaje)>                 Eliminar(int id);
    Task<EstadisticasConduccionDto>                      GetEstadisticasUsuario(int usuarioId);
}
