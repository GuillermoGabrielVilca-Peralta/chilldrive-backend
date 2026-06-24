using System;
using System.Collections.Generic;

namespace ChillDrive_Backend.Models;

/// <summary>
/// Catálogo de premios canjeables con puntos ChillDrive
/// </summary>
public partial class Premio
{
    public int Id { get; set; }

    public int AliadoId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public int PuntosRequeridos { get; set; }

    public int Stock { get; set; }

    public DateOnly? FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public string? Imagen { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public virtual Aliado Aliado { get; set; } = null!;

    public virtual ICollection<Canje> Canjes { get; set; } = new List<Canje>();
}
