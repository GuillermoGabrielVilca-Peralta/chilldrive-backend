namespace ChillDrive_Backend.Models.Constants;

/// <summary>
/// Fórmula compartida del puntaje de conducción 0–100.
/// Por categoría: <c>score = max(0, 100 + suma de impactos negativos de esa categoría)</c>.
/// General: promedio de las 5 categorías penalizables.
///
/// Es la MISMA fórmula que usa <c>EventoService.GetEstadisticasUsuario</c> (score por
/// usuario, desde datos agregados) y la app Flutter (tab Calificación). Aquí se calcula
/// por viaje, desde sus eventos, para persistir <c>Viaje.PuntajeConduccion</c> en Finalizar.
/// </summary>
public static class ScoreConduccion
{
    /// <summary>Tipos de evento que penalizan el puntaje, uno por categoría.</summary>
    public static readonly string[] TiposPenalizables =
    [
        "Exceso Velocidad", "Frenado Brusco", "Aceleración Brusca", "Giro Brusco", "Uso Celular"
    ];

    /// <summary>
    /// Puntaje general 0–100 calculado desde los eventos (tipo + impacto). Solo los
    /// impactos negativos de los tipos penalizables restan; el resto no afecta. Sin
    /// eventos penalizables → 100 (conducción perfecta).
    /// </summary>
    public static int General(IEnumerable<(string Tipo, int Impacto)> eventos)
    {
        var lista = eventos as ICollection<(string Tipo, int Impacto)> ?? eventos.ToList();
        var scores = TiposPenalizables.Select(tipo =>
        {
            var impactoNegativo = lista
                .Where(e => e.Tipo == tipo && e.Impacto < 0)
                .Sum(e => e.Impacto);
            return Math.Max(0, 100 + impactoNegativo);
        });
        return (int)scores.Average();
    }
}
