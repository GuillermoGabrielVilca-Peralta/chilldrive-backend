using System;
using System.Collections.Generic;

namespace ChillDrive_Backend.Models;

public partial class VSuscripcionesActiva
{
    public int? UsuarioId { get; set; }

    public string? Conductor { get; set; }

    public string? Correo { get; set; }

    public string? TipoConductor { get; set; }

    public int? SuscripcionId { get; set; }

    public DateOnly? FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public decimal? MontoSoles { get; set; }

    public string? MetodoPago { get; set; }

    public int? DiasRestantes { get; set; }
}
