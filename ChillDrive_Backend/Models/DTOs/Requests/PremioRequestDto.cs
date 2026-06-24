using System.ComponentModel.DataAnnotations;

namespace ChillDrive_Backend.Models.DTOs.Requests;

public class PremioRequestDto
{
    [Required(ErrorMessage = "El ID del aliado es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID del aliado debe ser mayor a 0.")]
    public int       AliadoId         { get; set; }

    [Required(ErrorMessage = "El nombre del premio es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    public string    Nombre           { get; set; } = null!;

    [MaxLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    public string?   Descripcion      { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Los puntos requeridos deben ser al menos 1.")]
    public int       PuntosRequeridos { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    public int       Stock            { get; set; } = 0;

    public DateOnly? FechaInicio      { get; set; }
    public DateOnly? FechaFin         { get; set; }

    [MaxLength(255, ErrorMessage = "La URL de imagen no puede superar los 255 caracteres.")]
    public string?   Imagen           { get; set; }

    // Valores alineados con PremioService.EstadosValidos
    [Required(ErrorMessage = "El estado es obligatorio.")]
    [RegularExpression("^(Disponible|Agotado|Vencido|Pausado)$",
        ErrorMessage = "Estado debe ser: Disponible, Agotado, Vencido o Pausado.")]
    public string    Estado           { get; set; } = "Disponible";
}
