using ChillDrive_Backend.Models.Constants;
using Xunit;

namespace ChillDrive_Backend.Tests;

// Prueba la fórmula pura del puntaje de conducción 0–100.
public class ScoreConduccionTests
{
    [Fact]
    public void SinEventos_Da100()
    {
        Assert.Equal(100, ScoreConduccion.General([]));
    }

    [Fact]
    public void UnFrenadoLeve_BajaSoloEsaCategoria()
    {
        // Frenado = max(0, 100-10) = 90; otras 4 categorías = 100 → promedio 98.
        var score = ScoreConduccion.General([("Frenado Brusco", -10)]);
        Assert.Equal(98, score);
    }

    [Fact]
    public void CategoriaSaturada_QuedaEnCero()
    {
        // Exceso Velocidad con -200 → categoría 0; promedio (0+100*4)/5 = 80.
        var score = ScoreConduccion.General([("Exceso Velocidad", -200)]);
        Assert.Equal(80, score);
    }

    [Fact]
    public void ImpactosPositivosYTiposNoPenalizables_NoAfectan()
    {
        var score = ScoreConduccion.General([
            ("Señal Respetada", 10),   // positivo, no penaliza
            ("Peatón Detectado", 0),   // neutro, no está entre los penalizables
        ]);
        Assert.Equal(100, score);
    }

    [Fact]
    public void VariosDeLaMismaCategoria_SeSuman()
    {
        // Dos frenados de -10 → -20 → categoría 80; promedio (80+100*4)/5 = 96.
        var score = ScoreConduccion.General([
            ("Frenado Brusco", -10),
            ("Frenado Brusco", -10),
        ]);
        Assert.Equal(96, score);
    }

    [Fact]
    public void VariasCategoriasAfectadas_PromediaTodas()
    {
        // Frenado 90, Giro 80, resto 100 → (90+80+100+100+100)/5 = 94.
        var score = ScoreConduccion.General([
            ("Frenado Brusco", -10),
            ("Giro Brusco", -20),
        ]);
        Assert.Equal(94, score);
    }
}
