using System;
using System.Collections.Generic;

namespace ChillDrive_Backend.Models;

public partial class VRankingConductore
{
    public int? UsuarioId { get; set; }

    public string? Conductor { get; set; }

    public string? TipoConductor { get; set; }

    public int? PuntosActuales { get; set; }

    public int? PuntosHistoricos { get; set; }

    public string? Nivel { get; set; }

    public decimal? PuntajePromedio { get; set; }

    public long? TotalViajes { get; set; }
}
