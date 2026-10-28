using System;
using System.Collections.Generic;

namespace ChillDrive_Backend.Models;

/// <summary>
/// Ciclos de pago del plan Profesional (15 soles/mes)
/// </summary>
public partial class Suscripcione
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }

    public decimal MontoSoles { get; set; }

    public string MetodoPago { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public string? ReferenciaPago { get; set; }

    public DateTime FechaRegistro { get; set; }

    /// <summary>
    /// Prueba | Mensual | Trimestral | Anual | Puntos
    /// </summary>
    public string Plan { get; set; } = null!;

    /// <summary>
    /// Días de plan Profesional que otorga (incluye los meses de regalo)
    /// </summary>
    public int Dias { get; set; }

    /// <summary>
    /// Código que el conductor presenta al pagar en efectivo
    /// </summary>
    public string? CodigoPago { get; set; }

    public int? PuntosUsados { get; set; }

    public DateTime? FechaAprobacion { get; set; }

    public string? MotivoRechazo { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
