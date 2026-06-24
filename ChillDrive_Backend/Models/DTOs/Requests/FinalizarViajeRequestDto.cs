using System.ComponentModel.DataAnnotations;

namespace ChillDrive_Backend.Models.DTOs.Requests;

public class FinalizarViajeRequestDto
{
    public DateTime FechaFin          { get; set; } = DateTime.UtcNow;

    [Range(0, 99999.99, ErrorMessage = "La distancia debe estar entre 0 y 99 999 km.")]
    public decimal  DistanciaKm       { get; set; }

    [Range(0, 1440, ErrorMessage = "La duración debe estar entre 0 y 1 440 minutos (24 h).")]
    public int      DuracionMinutos   { get; set; }

    // Puntaje de 0 a 100 calculado por la IA de detección vehicular
    [Range(0, 100, ErrorMessage = "El puntaje de conducción debe estar entre 0 y 100.")]
    public decimal  PuntajeConduccion { get; set; }

    // Los puntos ya NO se envían al finalizar: se acumularon en tiempo real con cada evento
    // durante el viaje (ver EventoService). El viaje guarda el neto calculado en el servidor.
}
