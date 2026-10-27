using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Models.DTOs.Mappings;

public static class ViajeMappings
{
    public static ViajeResponseDto ToResponse(this Viaje v) => new()
    {
        Id                = v.Id,
        UsuarioId         = v.UsuarioId,
        VehiculoId        = v.VehiculoId,
        FechaInicio       = v.FechaInicio,
        FechaFin          = v.FechaFin,
        DistanciaKm       = v.DistanciaKm,
        DuracionMinutos   = v.DuracionMinutos,
        PuntajeConduccion = v.PuntajeConduccion,
        PuntosGanados     = v.PuntosGanados,
        Estado            = v.Estado
    };

    public static Viaje ToEntity(this ViajeRequestDto dto) => new()
    {
        UsuarioId     = dto.UsuarioId,
        VehiculoId    = dto.VehiculoId,
        FechaInicio   = dto.FechaInicio,
        Estado        = "En Curso",
        PuntosGanados = 0
    };

    // Registra la última posición del viaje; `recibidaEn` la fija el servicio (UTC).
    public static void ApplyTo(this UbicacionViajeRequestDto dto, Viaje viaje, DateTime recibidaEn)
    {
        viaje.UltimaLatitud        = dto.Latitud;
        viaje.UltimaLongitud       = dto.Longitud;
        viaje.UltimaVelocidadKmh   = dto.VelocidadKmh;
        viaje.UltimaUbicacionFecha = recibidaEn;
    }
}
