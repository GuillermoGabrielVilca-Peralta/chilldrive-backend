using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Repositories;
using ChillDrive_Backend.Services.Implements;
using Moq;
using Xunit;

namespace ChillDrive_Backend.Tests;

// Cubre las validaciones de PremioService: existencia y estado del aliado,
// rango de fechas y la lógica de cambio de estado del premio.
public class PremioServiceTests
{
    private readonly Mock<IPremioRepository>  _premioRepo  = new();
    private readonly Mock<IAliadoRepository>  _aliadoRepo  = new();
    private readonly PremioService            _sut;

    public PremioServiceTests()
    {
        _sut = new PremioService(_premioRepo.Object, _aliadoRepo.Object);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ConAliado(int id = 1, string estado = "Activo") =>
        _aliadoRepo.Setup(r => r.GetById(id))
                   .ReturnsAsync(new Aliado { Id = id, Nombre = "BurgerFuel", Categoria = "Gastronomía", Estado = estado });

    private void ConPremio(int id = 10) =>
        _premioRepo.Setup(r => r.GetById(id))
                   .ReturnsAsync(new Premio { Id = id, AliadoId = 1, Nombre = "Café gratis", PuntosRequeridos = 200, Estado = "Disponible" });

    private static PremioRequestDto DtoValido(int aliadoId = 1) =>
        new() { AliadoId = aliadoId, Nombre = "Café gratis", PuntosRequeridos = 200, Stock = 50, Estado = "Disponible" };

    // ── Registrar ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Registrar_AliadoInexistente_Falla()
    {
        _aliadoRepo.Setup(r => r.GetById(1)).ReturnsAsync((Aliado?)null);

        var (success, _, _) = await _sut.Registrar(DtoValido());

        Assert.False(success);
        _premioRepo.Verify(r => r.Add(It.IsAny<Premio>()), Times.Never);
    }

    [Theory]
    [InlineData("Inactivo")]
    [InlineData("Suspendido")]
    public async Task Registrar_AliadoNoActivo_Falla(string estadoAliado)
    {
        ConAliado(1, estadoAliado);

        var (success, _, _) = await _sut.Registrar(DtoValido());

        Assert.False(success);
        _premioRepo.Verify(r => r.Add(It.IsAny<Premio>()), Times.Never);
    }

    [Fact]
    public async Task Registrar_FechaFinAnteriorAInicio_Falla()
    {
        ConAliado();
        var dto = DtoValido();
        dto.FechaInicio = new DateOnly(2025, 6, 1);
        dto.FechaFin    = new DateOnly(2025, 5, 1);   // fin < inicio

        var (success, _, _) = await _sut.Registrar(dto);

        Assert.False(success);
        _premioRepo.Verify(r => r.Add(It.IsAny<Premio>()), Times.Never);
    }

    [Fact]
    public async Task Registrar_AliadoActivoYFechasValidas_CreaPremioyDevuelveId()
    {
        ConAliado();
        _premioRepo.Setup(r => r.Add(It.IsAny<Premio>())).ReturnsAsync(15);

        var (success, _, id) = await _sut.Registrar(DtoValido());

        Assert.True(success);
        Assert.Equal(15, id);
        _premioRepo.Verify(r => r.Add(It.IsAny<Premio>()), Times.Once);
    }

    [Fact]
    public async Task Registrar_SoloFechaInicio_SinFechaFin_NoValidaRango()
    {
        ConAliado();
        _premioRepo.Setup(r => r.Add(It.IsAny<Premio>())).ReturnsAsync(16);
        var dto = DtoValido();
        dto.FechaInicio = new DateOnly(2025, 6, 1);
        dto.FechaFin    = null;

        var (success, _, _) = await _sut.Registrar(dto);

        // Sin FechaFin no hay rango que validar → debe crear el premio
        Assert.True(success);
    }

    // ── Actualizar ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Actualizar_PremioNoEncontrado_Falla()
    {
        _premioRepo.Setup(r => r.GetById(10)).ReturnsAsync((Premio?)null);

        var (success, _) = await _sut.Actualizar(10, DtoValido());

        Assert.False(success);
    }

    [Fact]
    public async Task Actualizar_AliadoInexistente_Falla()
    {
        ConPremio(10);
        _aliadoRepo.Setup(r => r.GetById(1)).ReturnsAsync((Aliado?)null);

        var (success, _) = await _sut.Actualizar(10, DtoValido());

        Assert.False(success);
        _premioRepo.Verify(r => r.Update(It.IsAny<Premio>()), Times.Never);
    }

    [Fact]
    public async Task Actualizar_FechaFinAnteriorAInicio_Falla()
    {
        ConPremio(10);
        ConAliado();
        var dto = DtoValido();
        dto.FechaInicio = new DateOnly(2025, 8, 1);
        dto.FechaFin    = new DateOnly(2025, 7, 1);

        var (success, _) = await _sut.Actualizar(10, dto);

        Assert.False(success);
        _premioRepo.Verify(r => r.Update(It.IsAny<Premio>()), Times.Never);
    }

    [Fact]
    public async Task Actualizar_Valido_ActualizaPremio()
    {
        ConPremio(10);
        ConAliado();

        var (success, _) = await _sut.Actualizar(10, DtoValido());

        Assert.True(success);
        _premioRepo.Verify(r => r.Update(It.IsAny<Premio>()), Times.Once);
    }

    // ── Eliminar ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Eliminar_PremioNoEncontrado_Falla()
    {
        _premioRepo.Setup(r => r.GetById(10)).ReturnsAsync((Premio?)null);

        var (success, _) = await _sut.Eliminar(10);

        Assert.False(success);
    }

    [Fact]
    public async Task Eliminar_Valido_EliminaPremio()
    {
        ConPremio(10);

        var (success, _) = await _sut.Eliminar(10);

        Assert.True(success);
        _premioRepo.Verify(r => r.Delete(It.IsAny<Premio>()), Times.Once);
    }

    // ── CambiarEstado ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CambiarEstado_EstadoInvalido_Falla()
    {
        var (success, _) = await _sut.CambiarEstado(10, "Archivado");

        Assert.False(success);
        _premioRepo.Verify(r => r.GetById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CambiarEstado_PremioNoEncontrado_Falla()
    {
        _premioRepo.Setup(r => r.GetById(10)).ReturnsAsync((Premio?)null);

        var (success, _) = await _sut.CambiarEstado(10, "Disponible");

        Assert.False(success);
    }

    [Theory]
    [InlineData("Disponible")]
    [InlineData("Agotado")]
    [InlineData("Vencido")]
    [InlineData("Pausado")]
    public async Task CambiarEstado_EstadoValido_ActualizaYPersiste(string estado)
    {
        var premio = new Premio { Id = 10, AliadoId = 1, Nombre = "Café", PuntosRequeridos = 200, Estado = "Disponible" };
        _premioRepo.Setup(r => r.GetById(10)).ReturnsAsync(premio);

        var (success, _) = await _sut.CambiarEstado(10, estado);

        Assert.True(success);
        Assert.Equal(estado, premio.Estado);
        _premioRepo.Verify(r => r.Update(premio), Times.Once);
    }
}
