using System.ComponentModel.DataAnnotations;

namespace ChillDrive_Backend.Models.DTOs.Requests;

public class CamaraRequestDto
{
    [Required(ErrorMessage = "El ID del vehículo es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID del vehículo debe ser mayor a 0.")]
    public int      VehiculoId   { get; set; }

    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string?  Nombre       { get; set; }

    // Dirección IP o URL de stream — solo limitamos longitud
    [MaxLength(50, ErrorMessage = "La dirección IP no puede superar los 50 caracteres.")]
    public string?  DireccionIp  { get; set; }

    // Valores exactos del CHECK chk_camaras_posicion de la BD.
    [Required(ErrorMessage = "La posición es obligatoria.")]
    [RegularExpression("^(Frontal|Trasera|Interior|Lateral)$",
        ErrorMessage = "Posicion debe ser: Frontal, Trasera, Interior o Lateral.")]
    public string   Posicion     { get; set; } = "Frontal";

    [Required(ErrorMessage = "El estado es obligatorio.")]
    [RegularExpression("^(Conectada|Desconectada|Error)$",
        ErrorMessage = "Estado debe ser: Conectada, Desconectada o Error.")]
    public string   Estado       { get; set; } = "Desconectada";
}
