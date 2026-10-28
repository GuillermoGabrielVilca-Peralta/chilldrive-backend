using System.ComponentModel.DataAnnotations;

namespace ChillDrive_Backend.Models.DTOs.Requests;

// El conductor elige un plan; el precio y la duración los define el servidor
// (PlanesSuscripcion), nunca el cliente.
public class SolicitarSuscripcionRequestDto
{
    // Prueba | Mensual | Trimestral | Anual | Puntos
    [Required(ErrorMessage = "El plan es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El plan no puede superar los 20 caracteres.")]
    public string  Plan            { get; set; } = null!;

    // Solo para planes con dinero: Yape | Efectivo
    [MaxLength(30, ErrorMessage = "El método de pago no puede superar los 30 caracteres.")]
    public string? MetodoPago      { get; set; }

    // Número de operación que muestra Yape al pagar (obligatorio si el método es Yape).
    [MaxLength(30, ErrorMessage = "El número de operación no puede superar los 30 caracteres.")]
    public string? NumeroOperacion { get; set; }
}
