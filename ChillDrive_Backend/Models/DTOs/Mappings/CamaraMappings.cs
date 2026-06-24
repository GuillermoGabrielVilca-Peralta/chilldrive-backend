using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Models.DTOs.Mappings;

public static class CamaraMappings
{
    public static CamaraResponseDto ToResponse(this Camara c) => new()
    {
        Id            = c.Id,
        VehiculoId    = c.VehiculoId,
        Nombre        = c.Nombre,
        DireccionIp   = c.DireccionIp,
        Posicion      = c.Posicion,
        Estado        = c.Estado,
        FechaRegistro = c.FechaRegistro
    };

    public static Camara ToEntity(this CamaraRequestDto dto) => new()
    {
        VehiculoId  = dto.VehiculoId,
        Nombre      = dto.Nombre,
        DireccionIp = dto.DireccionIp,
        Posicion    = dto.Posicion,
        Estado      = dto.Estado
    };

    public static void ApplyTo(this CamaraRequestDto dto, Camara camara)
    {
        camara.VehiculoId  = dto.VehiculoId;
        camara.Nombre      = dto.Nombre;
        camara.DireccionIp = dto.DireccionIp;
        camara.Posicion    = dto.Posicion;
        camara.Estado      = dto.Estado;
    }
}
