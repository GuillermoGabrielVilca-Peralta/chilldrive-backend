using System.ComponentModel.DataAnnotations;

namespace ChillDrive_Backend.Models.DTOs.Requests;

public class AcreditarPuntosRequestDto
{
    [Required(ErrorMessage = "El ID del usuario es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID del usuario debe ser mayor a 0.")]
    public int UsuarioId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Se debe acreditar al menos 1 punto.")]
    public int Puntos    { get; set; }
}
