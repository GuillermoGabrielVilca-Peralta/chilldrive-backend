using System.ComponentModel.DataAnnotations;

namespace ChillDrive_Backend.Models.DTOs.Requests;

// El administrador indica por qué no pudo verificar el pago (lo verá el conductor).
public class RechazarSuscripcionRequestDto
{
    [Required(ErrorMessage = "El motivo del rechazo es obligatorio.")]
    [MaxLength(255, ErrorMessage = "El motivo no puede superar los 255 caracteres.")]
    public string Motivo { get; set; } = null!;
}
