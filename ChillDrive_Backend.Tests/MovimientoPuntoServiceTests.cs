using ChillDrive_Backend.Models;
using ChillDrive_Backend.Repositories;
using ChillDrive_Backend.Services.Implements;
using Moq;
using Xunit;

namespace ChillDrive_Backend.Tests;

// MovimientoPuntoService es un lector del ledger (sin lógica de negocio): solo delega
// al repo y mapea. Se verifica el contrato de delegación, el caso null y que el delta
// con signo del movimiento se conserve al mapear. La ESCRITURA del ledger se cubre en
// PuntosUsuarioServiceTests y EventoServiceTests.
public class MovimientoPuntoServiceTests
{
    private readonly Mock<IMovimientoPuntoRepository> _repo = new();
    private readonly MovimientoPuntoService           _sut;

    public MovimientoPuntoServiceTests()
    {
        _sut = new MovimientoPuntoService(_repo.Object);
    }

    private static MovimientosPunto Mov(int id, int puntos, string tipo = "Ganado") => new()
    {
        Id              = id,
        UsuarioId       = 1,
        Tipo            = tipo,
        Puntos          = puntos,
        FechaMovimiento = DateTime.UtcNow,
    };

    [Fact]
    public async Task GetById_NoEncontrado_DevuelveNull()
    {
        _repo.Setup(r => r.GetById(9)).ReturnsAsync((MovimientosPunto?)null);

        var dto = await _sut.GetById(9);

        Assert.Null(dto);
    }

    [Fact]
    public async Task GetById_Existe_MapeaConservandoElSigno()
    {
        // Un ajuste por infracción guarda un delta negativo: debe conservarse.
        _repo.Setup(r => r.GetById(3)).ReturnsAsync(Mov(3, -10, "Ajuste"));

        var dto = await _sut.GetById(3);

        Assert.NotNull(dto);
        Assert.Equal(3, dto!.Id);
        Assert.Equal(-10, dto.Puntos);
        Assert.Equal("Ajuste", dto.Tipo);
    }

    [Fact]
    public async Task GetByUsuarioId_MapeaTodosLosMovimientos()
    {
        _repo.Setup(r => r.GetByUsuarioId(1)).ReturnsAsync(new List<MovimientosPunto>
        {
            Mov(1, 10),
            Mov(2, -5, "Ajuste"),
        });

        var lista = await _sut.GetByUsuarioId(1);

        Assert.Equal(2, lista.Count);
        Assert.Equal(10, lista[0].Puntos);
        Assert.Equal(-5, lista[1].Puntos);
    }

    [Fact]
    public async Task GetByUsuarioIdYTipo_PasaElTipoAlRepo()
    {
        _repo.Setup(r => r.GetByUsuarioIdYTipo(1, "Ganado"))
             .ReturnsAsync(new List<MovimientosPunto> { Mov(1, 10, "Ganado") });

        var lista = await _sut.GetByUsuarioIdYTipo(1, "Ganado");

        Assert.Single(lista);
        _repo.Verify(r => r.GetByUsuarioIdYTipo(1, "Ganado"), Times.Once);
    }
}
