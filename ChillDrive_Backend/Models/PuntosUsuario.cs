using System;
using System.Collections.Generic;

namespace ChillDrive_Backend.Models;

public partial class PuntosUsuario
{
    public int UsuarioId { get; set; }

    public int PuntosActuales { get; set; }

    /// <summary>
    /// Acumulado total histórico; no baja con canjes. Determina nivel y ranking.
    /// </summary>
    public int PuntosHistoricos { get; set; }

    public string Nivel { get; set; } = null!;

    public DateTime UltimaActualizacion { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
