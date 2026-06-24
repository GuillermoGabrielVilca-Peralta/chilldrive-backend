using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Repositories;
using ChillDrive_Backend.Services.Implements;
using Moq;
using Xunit;

namespace ChillDrive_Backend.Tests;

// Cubre las validaciones de negocio de CamaraService: existencia de vehículo,
// existencia de la cámara en actualizaciones/eliminaciones y la lógica de estados.
public class CamaraServiceTests
{
    private readonly Mock<ICamaraRepository>   _camaraRepo   = new();
    private readonly Mock<IVehiculoRepository> _vehiculoRepo = new();
    private readonly CamaraService             _sut;

    public CamaraServiceTests()
    {
        _sut = new CamaraService(_camaraRepo.Object, _vehiculoRepo.Object);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ConVehiculo(int id = 1) =>
        _vehiculoRepo.Setup(r => r.GetById(id))
                     .ReturnsAsync(new Vehiculo { Id = id, UsuarioId = 10 });

    private void ConCamara(int id = 5) =>
        _camaraRepo.Setup(r => r.GetById(id))
                   .ReturnsAsync(new Camara { Id = id, VehiculoId = 1, Posicion = "Frontal", Estado = "Desconectada" });

    private static CamaraRequestDto DtoValido(int vehiculoId = 1) =>
        new() { VehiculoId = vehiculoId, Posicion = "Frontal", Estado = "Desconectada" };

    // ── Registrar ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Registrar_VehiculoInexistente_Falla()
    {
        _vehiculoRepo.Setup(r => r.GetById(1)).ReturnsAsync((Vehiculo?)null);

        var (success, _, _) = await _sut.Registrar(DtoValido());

        Assert.False(success);
    }

    [Fact]
    public async Task Registrar_VehiculoValido_CreaYDevuelveId()
    {
        ConVehiculo();
        _camaraRepo.Setup(r => r.Add(It.IsAny<Camara>())).ReturnsAsync(7);

        var (success, _, id) = await _sut.Registrar(DtoValido());

        Assert.True(success);
        Assert.Equal(7, id);
        _camaraRepo.Verify(r => r.Add(It.IsAny<Camara>()), Times.Once);
    }

    // ── Actualizar ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Actualizar_CamaraNoEncontrada_Falla()
    {
        _camaraRepo.Setup(r => r.GetById(5)).ReturnsAsync((Camara?)null);

        var (success, _) = await _sut.Actualizar(5, DtoValido());

        Assert.False(success);
    }

    [Fact]
    public async Task Actualizar_VehiculoInexistente_Falla()
    {
        ConCamara(5);
        _vehiculoRepo.Setup(r => r.GetById(1)).ReturnsAsync((Vehiculo?)null);

        var (success, _) = await _sut.Actualizar(5, DtoValido());

        Assert.False(success);
    }

    [Fact]
    public async Task Actualizar_Valido_ActualizaCamara()
    {
        ConCamara(5);
        ConVehiculo();

        var (success, _) = await _sut.Actualizar(5, DtoValido());

        Assert.True(success);
        _camaraRepo.Verify(r => r.Update(It.IsAny<Camara>()), Times.Once);
    }

    // ── Eliminar ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Eliminar_CamaraNoEncontrada_Falla()
    {
        _camaraRepo.Setup(r => r.GetById(5)).ReturnsAsync((Camara?)null);

        var (success, _) = await _sut.Eliminar(5);

        Assert.False(success);
    }

    [Fact]
    public async Task Eliminar_Valido_EliminaCamara()
    {
        ConCamara(5);

        var (success, _) = await _sut.Eliminar(5);

        Assert.True(success);
        _camaraRepo.Verify(r => r.Delete(It.IsAny<Camara>()), Times.Once);
    }

    // ── CambiarEstado ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CambiarEstado_EstadoInvalido_Falla()
    {
        var (success, _) = await _sut.CambiarEstado(5, "Apagada");

        Assert.False(success);
        // No debe tocar el repositorio si el estado no es válido
        _camaraRepo.Verify(r => r.GetById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CambiarEstado_CamaraNoEncontrada_Falla()
    {
        _camaraRepo.Setup(r => r.GetById(5)).ReturnsAsync((Camara?)null);

        var (success, _) = await _sut.CambiarEstado(5, "Conectada");

        Assert.False(success);
    }

    [Theory]
    [InlineData("Conectada")]
    [InlineData("Desconectada")]
    [InlineData("Error")]
    public async Task CambiarEstado_EstadoValido_ActualizaYPersiste(string estado)
    {
        var camara = new Camara { Id = 5, VehiculoId = 1, Posicion = "Frontal", Estado = "Desconectada" };
        _camaraRepo.Setup(r => r.GetById(5)).ReturnsAsync(camara);

        var (success, _) = await _sut.CambiarEstado(5, estado);

        Assert.True(success);
        Assert.Equal(estado, camara.Estado);
        _camaraRepo.Verify(r => r.Update(camara), Times.Once);
    }
}
