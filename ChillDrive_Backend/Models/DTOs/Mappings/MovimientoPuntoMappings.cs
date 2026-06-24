using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Models.DTOs.Mappings;

public static class MovimientoPuntoMappings
{
    public static MovimientoPuntoResponseDto ToResponse(this MovimientosPunto m) => new()
    {
        Id              = m.Id,
        UsuarioId       = m.UsuarioId,
        NombreUsuario   = m.Usuario is not null
            ? $"{m.Usuario.Nombres} {m.Usuario.Apellidos}"
            : string.Empty,
        Tipo            = m.Tipo!,
        Puntos          = m.Puntos,
        Descripcion     = m.Descripcion,
        ViajeId         = m.ViajeId,
        CanjeId         = m.CanjeId,
        FechaMovimiento = m.FechaMovimiento
    };
}
