using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Repositories;
using ChillDrive_Backend.Services.Implements;
using Moq;
using Xunit;

namespace ChillDrive_Backend.Tests;

// Prueba el canje de premios: las validaciones de negocio y el camino feliz
// (descuento de puntos + creación del canje + registro en el ledger).
public class CanjeServiceTests
{
    private readonly Mock<ICanjeRepository>           _canjeRepo      = new();
    private readonly Mock<IPremioRepository>          _premioRepo     = new();
    private readonly Mock<IPuntosUsuarioRepository>   _puntosRepo     = new();
    private readonly Mock<IUsuarioRepository>         _usuarioRepo    = new();
    private readonly Mock<IMovimientoPuntoRepository> _movimientoRepo = new();
    private readonly CanjeService                     _sut;

    public CanjeServiceTests()
    {
        _sut = new CanjeService(
            _canjeRepo.Object, _premioRepo.Object, _puntosRepo.Object,
            _usuarioRepo.Object, _movimientoRepo.Object, new FakeUnitOfWork());
    }

    private void ConUsuario(int id = 1) =>
        _usuarioRepo.Setup(r => r.GetById(id)).ReturnsAsync(new Usuario { Id = id, Correo = "a@a.com" });

    private void ConPremio(Premio premio) =>
        _premioRepo.Setup(r => r.GetById(premio.Id)).ReturnsAsync(premio);

    private void ConSaldo(int usuarioId, int puntosActuales) =>
        _puntosRepo.Setup(r => r.GetByUsuarioId(usuarioId))
                   .ReturnsAsync(new PuntosUsuario { UsuarioId = usuarioId, PuntosActuales = puntosActuales });

    [Fact]
    public async Task Canjear_UsuarioInexistente_Falla()
    {
        _usuarioRepo.Setup(r => r.GetById(1)).ReturnsAsync((Usuario?)null);

        var (success, _, _) = await _sut.Canjear(new CanjeRequestDto { UsuarioId = 1, PremioId = 5 });

        Assert.False(success);
    }

    [Fact]
    public async Task Canjear_PremioNoDisponible_Falla()
    {
        ConUsuario();
        ConPremio(new Premio { Id = 5, Nombre = "Café", Estado = "Agotado", Stock = 0, PuntosRequeridos = 100 });

        var (success, _, _) = await _sut.Canjear(new CanjeRequestDto { UsuarioId = 1, PremioId = 5 });

        Assert.False(success);
    }

    [Fact]
    public async Task Canjear_PuntosInsuficientes_Falla()
    {
        ConUsuario();
        ConPremio(new Premio { Id = 5, Nombre = "Café", Estado = "Disponible", Stock = 10, PuntosRequeridos = 500 });
        ConSaldo(1, 100);

        var (success, _, _) = await _sut.Canjear(new CanjeRequestDto { UsuarioId = 1, PremioId = 5 });

        Assert.False(success);
        // No debe descontar puntos si el canje no procede.
        _puntosRepo.Verify(r => r.DescontarPuntos(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Canjear_PremioVencido_Falla()
    {
        ConUsuario();
        // FechaFin en el pasado → vencido.
        ConPremio(new Premio
        {
            Id = 5, Nombre = "Café", Estado = "Disponible", Stock = 10, PuntosRequeridos = 100,
            FechaFin = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
        });

        var (success, mensaje, _) = await _sut.Canjear(new CanjeRequestDto { UsuarioId = 1, PremioId = 5 });

        Assert.False(success);
        Assert.Contains("vencido", mensaje, StringComparison.OrdinalIgnoreCase);
        _puntosRepo.Verify(r => r.DescontarPuntos(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Canjear_PremioAunNoActivo_Falla()
    {
        ConUsuario();
        // FechaInicio en el futuro → aún no vigente.
        ConPremio(new Premio
        {
            Id = 5, Nombre = "Café", Estado = "Disponible", Stock = 10, PuntosRequeridos = 100,
            FechaInicio = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
        });

        var (success, _, _) = await _sut.Canjear(new CanjeRequestDto { UsuarioId = 1, PremioId = 5 });

        Assert.False(success);
        _puntosRepo.Verify(r => r.DescontarPuntos(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Canjear_Valido_DescuentaPuntosGeneraCanjeYLedger()
    {
        ConUsuario();
        ConPremio(new Premio { Id = 5, Nombre = "Café", Estado = "Disponible", Stock = 10, PuntosRequeridos = 200 });
        ConSaldo(1, 500);
        _canjeRepo.Setup(r => r.Add(It.IsAny<Canje>())).ReturnsAsync(42);

        var (success, _, id) = await _sut.Canjear(new CanjeRequestDto { UsuarioId = 1, PremioId = 5 });

        Assert.True(success);
        Assert.Equal(42, id);
        // Descuenta exactamente los puntos requeridos...
        _puntosRepo.Verify(r => r.DescontarPuntos(1, 200), Times.Once);
        // ...y registra el movimiento en el ledger como "Canjeado" con delta negativo.
        _movimientoRepo.Verify(r => r.Add(It.Is<MovimientosPunto>(
            m => m.Tipo == MovimientoTipos.Canjeado && m.Puntos == -200)), Times.Once);
    }
}
