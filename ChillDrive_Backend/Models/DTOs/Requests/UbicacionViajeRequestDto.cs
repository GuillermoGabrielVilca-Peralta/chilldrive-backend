using System.ComponentModel.DataAnnotations;

namespace ChillDrive_Backend.Models.DTOs.Requests;

// Posición que la app envía cada ~10 s durante un viaje en curso (monitoreo en vivo).
public class UbicacionViajeRequestDto
{
    [Range(-90, 90, ErrorMessage = "La latitud debe estar entre -90 y 90.")]
    public decimal  Latitud      { get; set; }

    [Range(-180, 180, ErrorMessage = "La longitud debe estar entre -180 y 180.")]
    public decimal  Longitud     { get; set; }

    // Opcional: el GPS puede no reportar velocidad en el primer fix.
    [Range(0, 300, ErrorMessage = "La velocidad debe estar entre 0 y 300 km/h.")]
    public decimal? VelocidadKmh { get; set; }
}
