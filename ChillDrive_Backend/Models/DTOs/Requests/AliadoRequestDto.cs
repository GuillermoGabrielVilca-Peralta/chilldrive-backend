using System.ComponentModel.DataAnnotations;

namespace ChillDrive_Backend.Models.DTOs.Requests;

public class AliadoRequestDto
{
    [Required(ErrorMessage = "El nombre del aliado es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string   Nombre             { get; set; } = null!;

    // RUC peruano: exactamente 11 dígitos
    [RegularExpression(@"^\d{11}$", ErrorMessage = "El RUC debe tener exactamente 11 dígitos.")]
    public string?  Ruc                { get; set; }

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    [MaxLength(50, ErrorMessage = "La categoría no puede superar los 50 caracteres.")]
    public string   Categoria          { get; set; } = "Otro";

    [MaxLength(200, ErrorMessage = "La dirección no puede superar los 200 caracteres.")]
    public string?  Direccion          { get; set; }

    [MaxLength(60, ErrorMessage = "El distrito no puede superar los 60 caracteres.")]
    public string?  Distrito           { get; set; }

    [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
    [MaxLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    public string?  Telefono           { get; set; }

    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    [MaxLength(150, ErrorMessage = "El email no puede superar los 150 caracteres.")]
    public string?  Email              { get; set; }

    [MaxLength(255, ErrorMessage = "La URL del logo no puede superar los 255 caracteres.")]
    public string?  Logo               { get; set; }

    [MaxLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    public string?  Descripcion        { get; set; }

    [Range(0, 100, ErrorMessage = "La comisión debe estar entre 0% y 100%.")]
    public decimal  ComisionPorcentaje { get; set; } = 10.00m;

    // Valores alineados con AliadoService.EstadosValidos
    [Required(ErrorMessage = "El estado es obligatorio.")]
    [RegularExpression("^(Activo|Inactivo|Suspendido)$",
        ErrorMessage = "Estado debe ser: Activo, Inactivo o Suspendido.")]
    public string   Estado             { get; set; } = "Activo";
}
