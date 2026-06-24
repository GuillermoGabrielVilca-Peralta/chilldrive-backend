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

    // Id único del evento generado en el cliente (UUID). Clave de idempotencia: un
    // reenvío de la cola offline con el mismo IdLocal no crea un evento duplicado.
    // Opcional: si no viene, el evento se registra sin protección de idempotencia.
    [MaxLength(64, ErrorMessage = "El idLocal no puede superar los 64 caracteres.")]
    public string?  IdLocal        { get; set; }

    // Hora en que el cliente DETECTÓ el evento (ISO-8601 UTC). Es la hora real del
    // hecho, no la de llegada al servidor: importa porque la cola offline puede
    // enviarse horas después. Sirve para ordenar bien la ruta y para la defensa
    // anti-flood. Opcional: si no viene o es inválida, el backend usa la hora actual.
    [MaxLength(40, ErrorMessage = "La fecha de detección no puede superar los 40 caracteres.")]
    public string?  FechaDeteccion { get; set; }

    // El impacto en puntos NO lo manda el cliente: lo decide el backend según el tipo de
    // evento (ver ImpactoEventos). Así se evita que se envíen puntos arbitrarios.

    [Range(-90, 90, ErrorMessage = "La latitud debe estar entre -90 y 90.")]
    public decimal? Latitud        { get; set; }

    [Range(-180, 180, ErrorMessage = "La longitud debe estar entre -180 y 180.")]
    public decimal? Longitud       { get; set; }

    [MaxLength(255, ErrorMessage = "La URL de evidencia no puede superar los 255 caracteres.")]
    public string?  FotoEvidencia  { get; set; }
}
