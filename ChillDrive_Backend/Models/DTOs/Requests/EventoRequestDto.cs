using System.ComponentModel.DataAnnotations;

namespace ChillDrive_Backend.Models.DTOs.Requests;

public class EventoRequestDto
{
    // ViajeId es nullable: un evento puede registrarse fuera de un viaje activo
    [Range(1, int.MaxValue, ErrorMessage = "El ID del viaje debe ser mayor a 0.")]
    public int?     ViajeId        { get; set; }

    [Required(ErrorMessage = "El ID de la cámara es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID de la cámara debe ser mayor a 0.")]
    public int      CamaraId       { get; set; }

    [Required(ErrorMessage = "El tipo de evento es obligatorio.")]
    [MaxLength(60, ErrorMessage = "El tipo de evento no puede superar los 60 caracteres.")]
    public string   TipoEvento     { get; set; } = null!;

    // El impacto en puntos NO lo manda el cliente: lo decide el backend según el tipo de
    // evento (ver ImpactoEventos). Así se evita que se envíen puntos arbitrarios.

    [Range(-90, 90, ErrorMessage = "La latitud debe estar entre -90 y 90.")]
    public decimal? Latitud        { get; set; }

    [Range(-180, 180, ErrorMessage = "La longitud debe estar entre -180 y 180.")]
    public decimal? Longitud       { get; set; }

    [MaxLength(255, ErrorMessage = "La URL de evidencia no puede superar los 255 caracteres.")]
    public string?  FotoEvidencia  { get; set; }
}
