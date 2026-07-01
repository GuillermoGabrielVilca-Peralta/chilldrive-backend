using System.ComponentModel.DataAnnotations;

namespace ChillDrive_Backend.Models.DTOs.Requests;

/// <summary>
/// Datos para renovar el acceso: el cliente envía el refresh token obtenido en el login.
/// El servicio lo valida y, si sigue vigente, emite un nuevo par de tokens.
/// </summary>
public class RefreshRequestDto
{
    [Required(ErrorMessage = "El refresh token es obligatorio.")]
    public string RefreshToken { get; set; } = null!;
}
