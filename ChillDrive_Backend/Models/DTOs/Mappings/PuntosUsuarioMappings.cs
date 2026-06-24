using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Models.DTOs.Mappings;

public static class PuntosUsuarioMappings
{
    public static PuntosUsuarioResponseDto ToResponse(this PuntosUsuario p) => new()
    {
        UsuarioId           = p.UsuarioId,
        NombreUsuario       = p.Usuario is not null
            ? $"{p.Usuario.Nombres} {p.Usuario.Apellidos}"
            : string.Empty,
        PuntosActuales      = p.PuntosActuales,
        PuntosHistoricos    = p.PuntosHistoricos,
        Nivel               = p.Nivel,
        UltimaActualizacion = p.UltimaActualizacion
    };
}
