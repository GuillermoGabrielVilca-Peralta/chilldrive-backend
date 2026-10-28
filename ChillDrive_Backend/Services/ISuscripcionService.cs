using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Services;

public interface ISuscripcionService
{
    Task<List<SuscripcionResponseDto>>                    GetAll();
    Task<List<SuscripcionResponseDto>>                    GetByUsuarioId(int usuarioId);
    Task<SuscripcionResponseDto?>                         GetById(int id);
    Task<SuscripcionResponseDto?>                         GetActivaByUsuarioId(int usuarioId);

    // Conductor
    Task<PlanesSuscripcionResponseDto>                    GetPlanes(int usuarioId);
    Task<EstadoSuscripcionResponseDto?>                   GetEstado(int usuarioId);
    Task<(bool success, string mensaje, int? id)>         Solicitar(int usuarioId, SolicitarSuscripcionRequestDto dto);
    Task<(bool success, string mensaje)>                  Cancelar(int id);

    // Administrador
    Task<List<SuscripcionResponseDto>>                    GetPendientes();
    Task<(bool success, string mensaje)>                  Aprobar(int id);
    Task<(bool success, string mensaje)>                  Rechazar(int id, string motivo);

    // Marca como vencidas las suscripciones cuyo periodo terminó y devuelve a Gratuito
    // a quien ya no tenga ninguna vigente. Devuelve cuántas venció.
    Task<int>                                             VencerSuscripciones();
}
