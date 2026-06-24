using System.ComponentModel.DataAnnotations;

namespace ChillDrive_Backend.Models.DTOs.Requests;

/// <summary>
/// Datos que el cliente envía para iniciar sesión.
/// Solo correo y contraseña en texto plano — el servicio verifica contra el hash en BD.
/// </summary>
public class LoginRequestDto
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Correo   { get; set; } = null!;

    // Solo validamos que no esté vacío; la longitud mínima no aplica aquí
    // porque no queremos rechazar passwords válidas de usuarios existentes.
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = null!;
}
