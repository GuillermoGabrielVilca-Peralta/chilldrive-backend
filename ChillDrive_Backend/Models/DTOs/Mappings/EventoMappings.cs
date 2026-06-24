using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Models.DTOs.Mappings;

public static class EventoMappings
{
    public static EventoResponseDto ToResponse(this Evento e) => new()
    {
        Id             = e.Id,
        ViajeId        = e.ViajeId,
        CamaraId       = e.CamaraId,
        TipoEvento     = e.TipoEvento,
        ImpactoPuntaje = e.ImpactoPuntaje,
        Latitud        = e.Latitud,
        Longitud       = e.Longitud,
        FotoEvidencia  = e.FotoEvidencia,
        FechaEvento    = e.FechaEvento
    };

    // El impacto en puntos (ImpactoEventos.Para) y la fecha resuelta (hora de detección
    // validada o, en su defecto, la actual) los calcula el servicio y se pasan aquí;
    // el mapper no decide reglas de negocio, solo arma la entidad.
    public static Evento ToEntity(this EventoRequestDto dto, int impactoPuntaje, DateTime fechaEvento) => new()
    {
        ViajeId         = dto.ViajeId,
        CamaraId        = dto.CamaraId,
        TipoEvento      = dto.TipoEvento,
        // Normaliza vacío → null para que el índice único ignore eventos sin idLocal.
        ClienteEventoId = string.IsNullOrWhiteSpace(dto.IdLocal) ? null : dto.IdLocal,
        ImpactoPuntaje  = impactoPuntaje,
        Latitud         = dto.Latitud,
        Longitud        = dto.Longitud,
        FotoEvidencia   = dto.FotoEvidencia,
        FechaEvento     = fechaEvento
    };
}
