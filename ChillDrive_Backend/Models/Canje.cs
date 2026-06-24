using System;
using System.Collections.Generic;

namespace ChillDrive_Backend.Models;

public partial class Canje
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public int PremioId { get; set; }

    public int PuntosUtilizados { get; set; }

    /// <summary>
    /// Hash hexadecimal de 32 chars generado con pgcrypto, único por canje
    /// </summary>
    public string CodigoQr { get; set; } = null!;

    public DateTime FechaCanje { get; set; }

    public DateTime? FechaUso { get; set; }

    public string Estado { get; set; } = null!;

    public virtual ICollection<MovimientosPunto> MovimientosPuntos { get; set; } = new List<MovimientosPunto>();

    public virtual Premio Premio { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
