using System;
using System.Collections.Generic;

namespace ChillDrive_Backend.Models;

/// <summary>
/// Dispositivos de captura de video por vehículo
/// </summary>
public partial class Camara
{
    public int Id { get; set; }

    public int VehiculoId { get; set; }

    public string? Nombre { get; set; }

    public string? DireccionIp { get; set; }

    public string Posicion { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<Evento> Eventos { get; set; } = new List<Evento>();

    public virtual Vehiculo Vehiculo { get; set; } = null!;
}
