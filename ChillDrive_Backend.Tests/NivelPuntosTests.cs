using ChillDrive_Backend.Models.Constants;
using Xunit;

namespace ChillDrive_Backend.Tests;

// Prueba la regla de niveles (función pura, sin dependencias).
public class NivelPuntosTests
{
    [Theory]
    [InlineData(0,     "Bronce")]
    [InlineData(299,   "Bronce")]
    [InlineData(300,   "Plata")]
    [InlineData(799,   "Plata")]
    [InlineData(800,   "Oro")]
    [InlineData(1999,  "Oro")]
    [InlineData(2000,  "Platino")]
    [InlineData(4999,  "Platino")]
    [InlineData(5000,  "Diamante")]
    [InlineData(10000, "Diamante")]
    public void Calcular_DevuelveNivelSegunPuntosHistoricos(int puntos, string nivelEsperado)
    {
        Assert.Equal(nivelEsperado, NivelPuntos.Calcular(puntos));
    }
}
