using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Models.DTOs.Mappings;

public static class PremioMappings
{
    public static PremioResponseDto ToResponse(this Premio p) => new()
    {
        Id               = p.Id,
        AliadoId         = p.AliadoId,
        NombreAliado     = p.Aliado?.Nombre ?? string.Empty,
        Nombre           = p.Nombre,
        Descripcion      = p.Descripcion,
        PuntosRequeridos = p.PuntosRequeridos,
        Stock            = p.Stock,
        FechaInicio      = p.FechaInicio,
        FechaFin         = p.FechaFin,
        Imagen           = p.Imagen,
        Estado           = p.Estado,
        FechaRegistro    = p.FechaRegistro
    };

    public static Premio ToEntity(this PremioRequestDto dto) => new()
    {
        AliadoId         = dto.AliadoId,
        Nombre           = dto.Nombre,
        Descripcion      = dto.Descripcion,
        PuntosRequeridos = dto.PuntosRequeridos,
        Stock            = dto.Stock,
        FechaInicio      = dto.FechaInicio,
        FechaFin         = dto.FechaFin,
        Imagen           = dto.Imagen,
        Estado           = dto.Estado
    };

    public static void ApplyTo(this PremioRequestDto dto, Premio premio)
    {
        premio.AliadoId         = dto.AliadoId;
        premio.Nombre           = dto.Nombre;
        premio.Descripcion      = dto.Descripcion;
        premio.PuntosRequeridos = dto.PuntosRequeridos;
        premio.Stock            = dto.Stock;
        premio.FechaInicio      = dto.FechaInicio;
        premio.FechaFin         = dto.FechaFin;
        premio.Imagen           = dto.Imagen;
        premio.Estado           = dto.Estado;
    }
}
