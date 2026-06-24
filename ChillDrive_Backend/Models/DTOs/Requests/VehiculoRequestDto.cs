using System.ComponentModel.DataAnnotations;

namespace ChillDrive_Backend.Models.DTOs.Requests;

public class VehiculoRequestDto
{
    [Required(ErrorMessage = "El ID del usuario es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID del usuario debe ser mayor a 0.")]
    public int      UsuarioId     { get; set; }

    [Required(ErrorMessage = "La placa es obligatoria.")]
    [MaxLength(15, ErrorMessage = "La placa no puede superar los 15 caracteres.")]
    public string   Placa         { get; set; } = null!;

    [MaxLength(50, ErrorMessage = "La marca no puede superar los 50 caracteres.")]
    public string?  Marca         { get; set; }

    [MaxLength(50, ErrorMessage = "El modelo no puede superar los 50 caracteres.")]
    public string?  Modelo        { get; set; }

    [Range(1980, 2100, ErrorMessage = "El año debe estar entre 1980 y 2100.")]
    public int?     Anio          { get; set; }

    [MaxLength(30, ErrorMessage = "El color no puede superar los 30 caracteres.")]
    public string?  Color         { get; set; }

    // Valores exactos del CHECK chk_vehiculos_tipo de la BD.
    [Required(ErrorMessage = "El tipo de vehículo es obligatorio.")]
    [RegularExpression("^(Auto|Moto|Camioneta|Bus|Taxi|Otro)$",
        ErrorMessage = "TipoVehiculo debe ser: Auto, Moto, Camioneta, Bus, Taxi u Otro.")]
    public string   TipoVehiculo  { get; set; } = "Auto";

    // Valores exactos del CHECK chk_vehiculos_estado de la BD.
    [Required(ErrorMessage = "El estado es obligatorio.")]
    [RegularExpression("^(Activo|Inactivo|Vendido)$",
        ErrorMessage = "Estado debe ser: Activo, Inactivo o Vendido.")]
    public string   Estado        { get; set; } = "Activo";
}
