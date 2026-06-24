using System.ComponentModel.DataAnnotations;

namespace ChillDrive_Backend.Models.DTOs.Requests;

public class ViajeRequestDto
{
    [Required(ErrorMessage = "El ID del usuario es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID del usuario debe ser mayor a 0.")]
    public int      UsuarioId   { get; set; }

    [Required(ErrorMessage = "El ID del vehículo es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID del vehículo debe ser mayor a 0.")]
    public int      VehiculoId  { get; set; }

    // El cliente puede omitir FechaInicio — se usa la hora del servidor como valor por defecto
    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
}
