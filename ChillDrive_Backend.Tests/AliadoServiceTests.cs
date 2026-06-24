using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Repositories;
using ChillDrive_Backend.Services.Implements;
using Moq;
using Xunit;

namespace ChillDrive_Backend.Tests;

// Cubre las validaciones de negocio de AliadoService: categoría válida,
// unicidad del RUC y la lógica de cambio de estado.
public class AliadoServiceTests
{
    private readonly Mock<IAliadoRepository> _repo = new();
    private readonly AliadoService           _sut;

    public AliadoServiceTests()
    {
        _sut = new AliadoService(_repo.Object);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ConAliado(int id = 1, string? ruc = null) =>
        _repo.Setup(r => r.GetById(id))
             .ReturnsAsync(new Aliado { Id = id, Nombre = "BurgerFuel", Categoria = "Gastronomía", Estado = "Activo", Ruc = ruc });

    private static AliadoRequestDto DtoValido(string? ruc = null) =>
        new() { Nombre = "BurgerFuel", Categoria = "Gastronomía", Estado = "Activo", Ruc = ruc };

    // ── Registrar ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Registrar_CategoriaInvalida_Falla()
    {
        var dto = DtoValido();
        dto.Categoria = "Farmacia";

        var (success, _, _) = await _sut.Registrar(dto);

        Assert.False(success);
        _repo.Verify(r => r.Add(It.IsAny<Aliado>()), Times.Never);
    }

    [Fact]
    public async Task Registrar_RucDuplicado_Falla()
    {
        _repo.Setup(r => r.ExistePorRuc("12345678901")).ReturnsAsync(true);

        var (success, _, _) = await _sut.Registrar(DtoValido(ruc: "12345678901"));

        Assert.False(success);
        _repo.Verify(r => r.Add(It.IsAny<Aliado>()), Times.Never);
    }

    [Fact]
    public async Task Registrar_SinRuc_NoVerificaDuplicadoYCreaAliado()
    {
        _repo.Setup(r => r.Add(It.IsAny<Aliado>())).ReturnsAsync(3);

        var (success, _, id) = await _sut.Registrar(DtoValido(ruc: null));

        Assert.True(success);
        Assert.Equal(3, id);
        // Sin RUC no debe consultar existencia
        _repo.Verify(r => r.ExistePorRuc(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Registrar_RucLibreYCategoriaValida_CreaAliado()
    {
        _repo.Setup(r => r.ExistePorRuc("12345678901")).ReturnsAsync(false);
        _repo.Setup(r => r.Add(It.IsAny<Aliado>())).ReturnsAsync(5);

        var (success, _, id) = await _sut.Registrar(DtoValido(ruc: "12345678901"));

        Assert.True(success);
        Assert.Equal(5, id);
        _repo.Verify(r => r.Add(It.IsAny<Aliado>()), Times.Once);
    }

    // ── Actualizar ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Actualizar_AliadoNoEncontrado_Falla()
    {
        _repo.Setup(r => r.GetById(1)).ReturnsAsync((Aliado?)null);

        var (success, _) = await _sut.Actualizar(1, DtoValido());

        Assert.False(success);
    }

    [Fact]
    public async Task Actualizar_CategoriaInvalida_Falla()
    {
        ConAliado(1);
        var dto = DtoValido();
        dto.Categoria = "Farmacia";

        var (success, _) = await _sut.Actualizar(1, dto);

        Assert.False(success);
        _repo.Verify(r => r.Update(It.IsAny<Aliado>()), Times.Never);
    }

    [Fact]
    public async Task Actualizar_RucEnUsoPorOtroAliado_Falla()
    {
        ConAliado(1, ruc: "11111111111");
        _repo.Setup(r => r.ExistePorRuc("99999999999")).ReturnsAsync(true);

        var (success, _) = await _sut.Actualizar(1, DtoValido(ruc: "99999999999"));

        Assert.False(success);
        _repo.Verify(r => r.Update(It.IsAny<Aliado>()), Times.Never);
    }

    [Fact]
    public async Task Actualizar_MismoRucQueYaTenia_NoConsultaDuplicado()
    {
        // Si el nuevo RUC es el mismo que ya tiene el aliado, no debe verificar duplicado.
        ConAliado(1, ruc: "12345678901");

        var (success, _) = await _sut.Actualizar(1, DtoValido(ruc: "12345678901"));

        Assert.True(success);
        _repo.Verify(r => r.ExistePorRuc(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Actualizar_RucLibreYCategoriaValida_ActualizaAliado()
    {
        ConAliado(1, ruc: "11111111111");
        _repo.Setup(r => r.ExistePorRuc("22222222222")).ReturnsAsync(false);

        var (success, _) = await _sut.Actualizar(1, DtoValido(ruc: "22222222222"));

        Assert.True(success);
        _repo.Verify(r => r.Update(It.IsAny<Aliado>()), Times.Once);
    }

    // ── Eliminar ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Eliminar_AliadoNoEncontrado_Falla()
    {
        _repo.Setup(r => r.GetById(1)).ReturnsAsync((Aliado?)null);

        var (success, _) = await _sut.Eliminar(1);

        Assert.False(success);
    }

    [Fact]
    public async Task Eliminar_Valido_EliminaAliado()
    {
        ConAliado(1);

        var (success, _) = await _sut.Eliminar(1);

        Assert.True(success);
        _repo.Verify(r => r.Delete(It.IsAny<Aliado>()), Times.Once);
    }

    // ── CambiarEstado ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CambiarEstado_EstadoInvalido_Falla()
    {
        var (success, _) = await _sut.CambiarEstado(1, "Archivado");

        Assert.False(success);
        _repo.Verify(r => r.GetById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CambiarEstado_AliadoNoEncontrado_Falla()
    {
        _repo.Setup(r => r.GetById(1)).ReturnsAsync((Aliado?)null);

        var (success, _) = await _sut.CambiarEstado(1, "Activo");

        Assert.False(success);
    }

    [Theory]
    [InlineData("Activo")]
    [InlineData("Inactivo")]
    [InlineData("Suspendido")]
    public async Task CambiarEstado_EstadoValido_ActualizaYPersiste(string estado)
    {
        var aliado = new Aliado { Id = 1, Nombre = "BurgerFuel", Categoria = "Gastronomía", Estado = "Activo" };
        _repo.Setup(r => r.GetById(1)).ReturnsAsync(aliado);

        var (success, _) = await _sut.CambiarEstado(1, estado);

        Assert.True(success);
        Assert.Equal(estado, aliado.Estado);
        _repo.Verify(r => r.Update(aliado), Times.Once);
    }
}
