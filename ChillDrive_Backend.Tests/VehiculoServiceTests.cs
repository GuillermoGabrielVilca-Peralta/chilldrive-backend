using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Repositories;
using ChillDrive_Backend.Services.Implements;
using Moq;
using Xunit;

namespace ChillDrive_Backend.Tests;

// Cubre las validaciones de VehiculoService: existencia del usuario, placa única
// (incluyendo el atajo de "misma placa" al actualizar) y existencia al eliminar.
public class VehiculoServiceTests
{
    private readonly Mock<IVehiculoRepository> _vehiculoRepo = new();
    private readonly Mock<IUsuarioRepository>  _usuarioRepo  = new();
    private readonly VehiculoService           _sut;

    public VehiculoServiceTests()
    {
        _sut = new VehiculoService(_vehiculoRepo.Object, _usuarioRepo.Object);
    }

    private void ConUsuario(int id = 1) =>
        _usuarioRepo.Setup(r => r.GetById(id)).ReturnsAsync(new Usuario { Id = id, Correo = "a@a.com" });

    private static VehiculoRequestDto DtoValido(string placa = "ABC123") =>
        new() { UsuarioId = 1, Placa = placa };

    // ── Registrar ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Registrar_UsuarioInexistente_Falla()
    {
        _usuarioRepo.Setup(r => r.GetById(1)).ReturnsAsync((Usuario?)null);

        var (success, _, _) = await _sut.Registrar(DtoValido());

        Assert.False(success);
    }

    [Fact]
    public async Task Registrar_PlacaDuplicada_Falla()
    {
        ConUsuario();
        _vehiculoRepo.Setup(r => r.ExistePorPlaca("ABC123")).ReturnsAsync(true);

        var (success, _, _) = await _sut.Registrar(DtoValido());

        Assert.False(success);
        _vehiculoRepo.Verify(r => r.Add(It.IsAny<Vehiculo>()), Times.Never);
    }

    [Fact]
    public async Task Registrar_Valido_CreaYDevuelveId()
    {
        ConUsuario();
        _vehiculoRepo.Setup(r => r.ExistePorPlaca("ABC123")).ReturnsAsync(false);
        _vehiculoRepo.Setup(r => r.Add(It.IsAny<Vehiculo>())).ReturnsAsync(7);

        var (success, _, id) = await _sut.Registrar(DtoValido());

        Assert.True(success);
        Assert.Equal(7, id);
        _vehiculoRepo.Verify(r => r.Add(It.IsAny<Vehiculo>()), Times.Once);
    }

    // ── Actualizar ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Actualizar_NoEncontrado_Falla()
    {
        _vehiculoRepo.Setup(r => r.GetById(5)).ReturnsAsync((Vehiculo?)null);

        var (success, _) = await _sut.Actualizar(5, DtoValido());

        Assert.False(success);
    }

    [Fact]
    public async Task Actualizar_PlacaEnUsoPorOtro_Falla()
    {
        _vehiculoRepo.Setup(r => r.GetById(5))
                     .ReturnsAsync(new Vehiculo { Id = 5, UsuarioId = 1, Placa = "VIEJA1" });
        _vehiculoRepo.Setup(r => r.ExistePorPlaca("NUEVA2")).ReturnsAsync(true);

        var (success, _) = await _sut.Actualizar(5, DtoValido("NUEVA2"));

        Assert.False(success);
        _vehiculoRepo.Verify(r => r.Update(It.IsAny<Vehiculo>()), Times.Never);
    }

    [Fact]
    public async Task Actualizar_MismaPlaca_NoChequeaDuplicadoYActualiza()
    {
        _vehiculoRepo.Setup(r => r.GetById(5))
                     .ReturnsAsync(new Vehiculo { Id = 5, UsuarioId = 1, Placa = "ABC123" });

        var (success, _) = await _sut.Actualizar(5, DtoValido("ABC123"));

        Assert.True(success);
        _vehiculoRepo.Verify(r => r.ExistePorPlaca(It.IsAny<string>()), Times.Never);
        _vehiculoRepo.Verify(r => r.Update(It.IsAny<Vehiculo>()), Times.Once);
    }

    // ── Eliminar ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Eliminar_NoEncontrado_Falla()
    {
        _vehiculoRepo.Setup(r => r.GetById(5)).ReturnsAsync((Vehiculo?)null);

        var (success, _) = await _sut.Eliminar(5);

        Assert.False(success);
    }

    [Fact]
    public async Task Eliminar_Valido_EliminaVehiculo()
    {
        _vehiculoRepo.Setup(r => r.GetById(5))
                     .ReturnsAsync(new Vehiculo { Id = 5, UsuarioId = 1, Placa = "ABC123" });

        var (success, _) = await _sut.Eliminar(5);

        Assert.True(success);
        _vehiculoRepo.Verify(r => r.Delete(It.IsAny<Vehiculo>()), Times.Once);
    }
}
