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

    // El impacto en puntos lo calcula el servicio (ImpactoEventos.Para) y se pasa aquí;
    // el mapper no decide reglas de negocio, solo arma la entidad.
    public static Evento ToEntity(this EventoRequestDto dto, int impactoPuntaje) => new()
    {
        ViajeId        = dto.ViajeId,
        CamaraId       = dto.CamaraId,
        TipoEvento     = dto.TipoEvento,
        ImpactoPuntaje = impactoPuntaje,
        Latitud        = dto.Latitud,
        Longitud       = dto.Longitud,
        FotoEvidencia  = dto.FotoEvidencia,
        FechaEvento    = DateTime.UtcNow
    };
}
