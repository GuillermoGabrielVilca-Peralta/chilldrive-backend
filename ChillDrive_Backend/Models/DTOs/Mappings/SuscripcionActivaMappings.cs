using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Models.DTOs.Mappings;

public static class SuscripcionActivaMappings
{
    public static SuscripcionActivaResponseDto ToResponse(this VSuscripcionesActiva v) => new()
    {
        UsuarioId     = v.UsuarioId     ?? 0,
        Conductor     = v.Conductor     ?? string.Empty,
        Correo        = v.Correo        ?? string.Empty,
        TipoConductor = v.TipoConductor ?? string.Empty,
        SuscripcionId = v.SuscripcionId ?? 0,
        FechaInicio   = v.FechaInicio,
        FechaFin      = v.FechaFin,
        MontoSoles    = v.MontoSoles    ?? 0m,
        MetodoPago    = v.MetodoPago    ?? string.Empty,
        DiasRestantes = v.DiasRestantes ?? 0
    };
}
