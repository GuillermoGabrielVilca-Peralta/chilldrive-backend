using System.ComponentModel.DataAnnotations;

namespace ChillDrive_Backend.Models.DTOs.Requests;

public class CanjeRequestDto
{
    [Required(ErrorMessage = "El ID del usuario es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID del usuario debe ser mayor a 0.")]
    public int UsuarioId { get; set; }

    [Required(ErrorMessage = "El ID del premio es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID del premio debe ser mayor a 0.")]
    public int PremioId  { get; set; }
}
