using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Models.DTOs.Mappings;

public static class VehiculoMappings
{
    public static VehiculoResponseDto ToResponse(this Vehiculo v) => new()
    {
        Id            = v.Id,
        UsuarioId     = v.UsuarioId,
        Placa         = v.Placa,
        Marca         = v.Marca,
        Modelo        = v.Modelo,
        Anio          = v.Anio,
        Color         = v.Color,
        TipoVehiculo  = v.TipoVehiculo,
        Estado        = v.Estado,
        FechaRegistro = v.FechaRegistro
    };

    public static Vehiculo ToEntity(this VehiculoRequestDto dto) => new()
    {
        UsuarioId    = dto.UsuarioId,
        Placa        = dto.Placa,
        Marca        = dto.Marca,
        Modelo       = dto.Modelo,
        Anio         = dto.Anio,
        Color        = dto.Color,
        TipoVehiculo = dto.TipoVehiculo,
        Estado       = dto.Estado
    };

    public static void ApplyTo(this VehiculoRequestDto dto, Vehiculo vehiculo)
    {
        vehiculo.UsuarioId    = dto.UsuarioId;
        vehiculo.Placa        = dto.Placa;
        vehiculo.Marca        = dto.Marca;
        vehiculo.Modelo       = dto.Modelo;
        vehiculo.Anio         = dto.Anio;
        vehiculo.Color        = dto.Color;
        vehiculo.TipoVehiculo = dto.TipoVehiculo;
        vehiculo.Estado       = dto.Estado;
    }
}
