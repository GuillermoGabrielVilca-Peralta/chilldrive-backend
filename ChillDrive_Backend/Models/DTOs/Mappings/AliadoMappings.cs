using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Models.DTOs.Mappings;

public static class AliadoMappings
{
    public static AliadoResponseDto ToResponse(this Aliado a) => new()
    {
        Id                 = a.Id,
        Nombre             = a.Nombre,
        Ruc                = a.Ruc,
        Categoria          = a.Categoria,
        Direccion          = a.Direccion,
        Distrito           = a.Distrito,
        Telefono           = a.Telefono,
        Email              = a.Email,
        Logo               = a.Logo,
        Descripcion        = a.Descripcion,
        ComisionPorcentaje = a.ComisionPorcentaje,
        Estado             = a.Estado,
        FechaRegistro      = a.FechaRegistro
    };

    public static Aliado ToEntity(this AliadoRequestDto dto) => new()
    {
        Nombre             = dto.Nombre,
        Ruc                = dto.Ruc,
        Categoria          = dto.Categoria,
        Direccion          = dto.Direccion,
        Distrito           = dto.Distrito,
        Telefono           = dto.Telefono,
        Email              = dto.Email,
        Logo               = dto.Logo,
        Descripcion        = dto.Descripcion,
        ComisionPorcentaje = dto.ComisionPorcentaje,
        Estado             = dto.Estado
    };

    public static void ApplyTo(this AliadoRequestDto dto, Aliado aliado)
    {
        aliado.Nombre             = dto.Nombre;
        aliado.Ruc                = dto.Ruc;
        aliado.Categoria          = dto.Categoria;
        aliado.Direccion          = dto.Direccion;
        aliado.Distrito           = dto.Distrito;
        aliado.Telefono           = dto.Telefono;
        aliado.Email              = dto.Email;
        aliado.Logo               = dto.Logo;
        aliado.Descripcion        = dto.Descripcion;
        aliado.ComisionPorcentaje = dto.ComisionPorcentaje;
        aliado.Estado             = dto.Estado;
    }
}
