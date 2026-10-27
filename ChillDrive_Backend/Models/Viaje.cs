using System;
using System.Collections.Generic;

namespace ChillDrive_Backend.Models;

public partial class Viaje
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public int VehiculoId { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public decimal? DistanciaKm { get; set; }

    public int? DuracionMinutos { get; set; }

    /// <summary>
    /// Score 0-100 generado por modelo de IA en tiempo real
    /// </summary>
    public decimal? PuntajeConduccion { get; set; }

    /// <summary>
    /// Puntos ChillDrive acreditados al finalizar el viaje
    /// </summary>
    public int PuntosGanados { get; set; }

    public string Estado { get; set; } = null!;

    // Última posición enviada por la app durante el viaje (monitoreo en vivo).
    public decimal? UltimaLatitud { get; set; }

    public decimal? UltimaLongitud { get; set; }

    public decimal? UltimaVelocidadKmh { get; set; }

    /// <summary>
    /// Momento (UTC) de la última posición recibida; el panel la usa para saber si el vehículo sigue transmitiendo
    /// </summary>
    public DateTime? UltimaUbicacionFecha { get; set; }

    public virtual ICollection<Evento> Eventos { get; set; } = new List<Evento>();

    public virtual ICollection<MovimientosPunto> MovimientosPuntos { get; set; } = new List<MovimientosPunto>();

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual Vehiculo Vehiculo { get; set; } = null!;
}
