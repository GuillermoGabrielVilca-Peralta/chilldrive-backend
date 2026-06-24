using ChillDrive_Backend.Services.Implements;
using Xunit;

namespace ChillDrive_Backend.Tests;

// Prueba el hashing BCrypt (servicio sin dependencias).
public class PasswordServiceTests
{
    private readonly PasswordService _sut = new();

    [Fact]
    public void Verificar_ConPasswordCorrecta_DevuelveTrue()
    {
        var hash = _sut.Hash("MiClave123");
        Assert.True(_sut.Verificar("MiClave123", hash));
    }

    [Fact]
    public void Verificar_ConPasswordIncorrecta_DevuelveFalse()
    {
        var hash = _sut.Hash("MiClave123");
        Assert.False(_sut.Verificar("ClaveEquivocada", hash));
    }

    [Fact]
    public void Hash_DeLaMismaPassword_GeneraHashesDistintos()
    {
        // BCrypt usa un salt aleatorio, así que dos hashes de la misma clave nunca son iguales.
        Assert.NotEqual(_sut.Hash("MiClave123"), _sut.Hash("MiClave123"));
    }
}
