using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Models.DTOs.Mappings;

public static class CanjeMappings
{
    public static CanjeResponseDto ToResponse(this Canje c) => new()
    {
        Id               = c.Id,
        UsuarioId        = c.UsuarioId,
        PremioId         = c.PremioId,
        NombrePremio     = c.Premio?.Nombre ?? string.Empty,
        NombreAliado     = c.Premio?.Aliado?.Nombre ?? string.Empty,
        PuntosUtilizados = c.PuntosUtilizados,
        CodigoQr         = c.CodigoQr,
        FechaCanje       = c.FechaCanje,
        FechaUso         = c.FechaUso,
        Estado           = c.Estado
    };
}
