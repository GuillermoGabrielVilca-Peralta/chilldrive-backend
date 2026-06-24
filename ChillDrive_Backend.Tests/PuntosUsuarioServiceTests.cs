using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Repositories;
using ChillDrive_Backend.Services.Implements;
using Moq;
using Xunit;

namespace ChillDrive_Backend.Tests;

// Prueba la acreditación de puntos: validaciones + que se escriba el ledger.
public class PuntosUsuarioServiceTests
{
    private readonly Mock<IPuntosUsuarioRepository>   _puntosRepo     = new();
    private readonly Mock<IUsuarioRepository>         _usuarioRepo    = new();
    private readonly Mock<IMovimientoPuntoRepository> _movimientoRepo = new();
    private readonly PuntosUsuarioService             _sut;

    public PuntosUsuarioServiceTests()
    {
        _sut = new PuntosUsuarioService(
            _puntosRepo.Object, _usuarioRepo.Object, _movimientoRepo.Object, new FakeUnitOfWork());
    }

    [Fact]
    public async Task AcreditarPuntos_UsuarioInexistente_Falla()
    {
        _usuarioRepo.Setup(r => r.GetById(99)).ReturnsAsync((Usuario?)null);

        var (success, mensaje) = await _sut.AcreditarPuntos(99, 100);

        Assert.False(success);
        Assert.Equal("El usuario no existe.", mensaje);
    }

    [Fact]
    public async Task AcreditarPuntos_PuntosNoPositivos_Falla()
    {
        _usuarioRepo.Setup(r => r.GetById(1)).ReturnsAsync(new Usuario { Id = 1, Correo = "a@a.com" });

        var (success, _) = await _sut.AcreditarPuntos(1, 0);

        Assert.False(success);
    }

    [Fact]
    public async Task AcreditarPuntos_Valido_AcreditaSaldoYRegistraLedger()
    {
        _usuarioRepo.Setup(r => r.GetById(1)).ReturnsAsync(new Usuario { Id = 1, Correo = "a@a.com" });

        var (success, _) = await _sut.AcreditarPuntos(1, 250);

        Assert.True(success);
        // El saldo se actualiza...
        _puntosRepo.Verify(r => r.AgregarOActualizar(1, 250), Times.Once);
        // ...y queda registrado en el ledger como "Ganado" con el monto correcto.
        _movimientoRepo.Verify(r => r.Add(It.Is<MovimientosPunto>(
            m => m.Tipo == MovimientoTipos.Ganado && m.Puntos == 250 && m.UsuarioId == 1)), Times.Once);
    }
}
