using System.ComponentModel.DataAnnotations;

namespace ChillDrive_Backend.Models.DTOs.Requests;

public class UsuarioRequestDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string  Nombres       { get; set; } = null!;

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [MaxLength(100, ErrorMessage = "Los apellidos no pueden superar los 100 caracteres.")]
    public string  Apellidos     { get; set; } = null!;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [MaxLength(150, ErrorMessage = "El correo no puede superar los 150 caracteres.")]
    public string  Correo        { get; set; } = null!;

    /// <summary>
    /// Contraseña en texto plano que envía el cliente.
    /// El servicio la hashea con BCrypt antes de persistirla — nunca se guarda en claro.
    /// </summary>
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [MaxLength(100, ErrorMessage = "La contraseña no puede superar los 100 caracteres.")]
    public string  Password      { get; set; } = null!;

    [MaxLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
    public string? Telefono      { get; set; }

    [MaxLength(255, ErrorMessage = "La URL de foto no puede superar los 255 caracteres.")]
    public string? FotoPerfil    { get; set; }

    // TipoPlan y Estado NO se reciben del cliente: el plan solo cambia por una suscripción
    // pagada y aprobada (SuscripcionService), y el estado solo lo cambia un administrador.
    // Antes eran editables aquí y cualquiera podía darse el plan Profesional o reactivarse
    // estando suspendido.

    // Valores exactos del CHECK chk_usuarios_conductor de la BD.
    [Required(ErrorMessage = "El tipo de conductor es obligatorio.")]
    [RegularExpression("^(Particular|Taxi|Delivery|Mototaxi|Otro)$",
        ErrorMessage = "TipoConductor debe ser: Particular, Taxi, Delivery, Mototaxi u Otro.")]
    public string  TipoConductor { get; set; } = "Particular";
}
