using System;
using System.Collections.Generic;

namespace ChillDrive_Backend.Models;

/// <summary>
/// Vehículos registrados por cada conductor
/// </summary>
public partial class Vehiculo
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public string Placa { get; set; } = null!;

    public string? Marca { get; set; }

    public string? Modelo { get; set; }

    public int? Anio { get; set; }

    public string? Color { get; set; }

    public string TipoVehiculo { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<Camara> Camaras { get; set; } = new List<Camara>();

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual ICollection<Viaje> Viajes { get; set; } = new List<Viaje>();
}
