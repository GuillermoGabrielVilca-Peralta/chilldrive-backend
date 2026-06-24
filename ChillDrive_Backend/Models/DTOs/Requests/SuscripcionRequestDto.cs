using System.ComponentModel.DataAnnotations;

namespace ChillDrive_Backend.Models.DTOs.Requests;

public class SuscripcionRequestDto
{
    [Required(ErrorMessage = "El ID del usuario es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID del usuario debe ser mayor a 0.")]
    public int      UsuarioId      { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    public DateOnly FechaInicio    { get; set; }

    [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
    public DateOnly FechaFin       { get; set; }

    [Range(0, 9999.99, ErrorMessage = "El monto debe estar entre 0 y 9 999.99 soles.")]
    public decimal  MontoSoles     { get; set; } = 15.00m;

    [Required(ErrorMessage = "El método de pago es obligatorio.")]
    [MaxLength(30, ErrorMessage = "El método de pago no puede superar los 30 caracteres.")]
    public string   MetodoPago     { get; set; } = "Yape";

    [MaxLength(100, ErrorMessage = "La referencia de pago no puede superar los 100 caracteres.")]
    public string?  ReferenciaPago { get; set; }
}
