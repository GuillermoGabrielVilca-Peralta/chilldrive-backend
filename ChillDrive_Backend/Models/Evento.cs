using System;
using System.Collections.Generic;

namespace ChillDrive_Backend.Models;

public partial class Evento
{
    public int Id { get; set; }

    /// <summary>
    /// NULL si la detección ocurre fuera de un viaje activo
    /// </summary>
    public int? ViajeId { get; set; }

    public int CamaraId { get; set; }

    public string TipoEvento { get; set; } = null!;

    /// <summary>
    /// Id único generado en el cliente (UUID) para idempotencia: si llega dos veces
    /// el mismo (reenvío de la cola offline), el evento NO se duplica. NULL para
    /// eventos generados sin cliente (p. ej. detecciones server-side).
    /// </summary>
    public string? ClienteEventoId { get; set; }

    /// <summary>
    /// Positivo = suma al puntaje del viaje; negativo = resta
    /// </summary>
    public int ImpactoPuntaje { get; set; }

    public decimal? Latitud { get; set; }

    public decimal? Longitud { get; set; }

    public string? FotoEvidencia { get; set; }

    public DateTime FechaEvento { get; set; }

    public virtual Camara Camara { get; set; } = null!;

    public virtual Viaje? Viaje { get; set; }
}
