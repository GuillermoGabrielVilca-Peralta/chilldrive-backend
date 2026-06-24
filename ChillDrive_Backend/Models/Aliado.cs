using System;
using System.Collections.Generic;

namespace ChillDrive_Backend.Models;

public partial class Aliado
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Ruc { get; set; }

    public string Categoria { get; set; } = null!;

    public string? Direccion { get; set; }

    public string? Distrito { get; set; }

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    public string? Logo { get; set; }

    public string? Descripcion { get; set; }

    /// <summary>
    /// % que ChillDrive cobra al aliado por cada canje redimido
    /// </summary>
    public decimal ComisionPorcentaje { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<Premio> Premios { get; set; } = new List<Premio>();
}
