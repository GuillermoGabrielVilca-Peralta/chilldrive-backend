using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Models.DTOs.Mappings;

public static class SuscripcionMappings
{
    public static SuscripcionResponseDto ToResponse(this Suscripcione s) => new()
    {
        Id             = s.Id,
        UsuarioId      = s.UsuarioId,
        NombreUsuario  = s.Usuario is not null
            ? $"{s.Usuario.Nombres} {s.Usuario.Apellidos}"
            : string.Empty,
        FechaInicio    = s.FechaInicio,
        FechaFin       = s.FechaFin,
        MontoSoles     = s.MontoSoles,
        MetodoPago     = s.MetodoPago,
        Estado         = s.Estado,
        ReferenciaPago = s.ReferenciaPago,
        FechaRegistro  = s.FechaRegistro
    };

    public static Suscripcione ToEntity(this SuscripcionRequestDto dto) => new()
    {
        UsuarioId      = dto.UsuarioId,
        FechaInicio    = dto.FechaInicio,
        FechaFin       = dto.FechaFin,
        MontoSoles     = dto.MontoSoles,
        MetodoPago     = dto.MetodoPago,
        Estado         = "Activa",
        ReferenciaPago = dto.ReferenciaPago
    };
}
