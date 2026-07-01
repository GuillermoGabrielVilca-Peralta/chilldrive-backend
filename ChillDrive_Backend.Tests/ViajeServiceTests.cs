using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Repositories;
using ChillDrive_Backend.Services.Implements;
using Moq;
using Xunit;

namespace ChillDrive_Backend.Tests;

// Prueba el ciclo de vida del viaje: iniciar (con validación de pertenencia del vehículo),
// finalizar (neto de puntos desde los eventos) y cancelar.
public class ViajeServiceTests
{
    private readonly Mock<IViajeRepository>    _viajeRepo    = new();
    private readonly Mock<IUsuarioRepository>  _usuarioRepo  = new();
    private readonly Mock<IVehiculoRepository> _vehiculoRepo = new();
    private readonly Mock<IEventoRepository>   _eventoRepo   = new();
    private readonly ViajeService              _sut;

    public ViajeServiceTests()
    {
        _sut = new ViajeService(
            _viajeRepo.Object, _usuarioRepo.Object, _vehiculoRepo.Object, _eventoRepo.Object);
    }

    [Fact]
    public async Task Iniciar_UsuarioInexistente_Falla()
    {
        _usuarioRepo.Setup(r => r.GetById(1)).ReturnsAsync((Usuario?)null);

        var (success, _, _) = await _sut.Iniciar(new ViajeRequestDto { UsuarioId = 1, VehiculoId = 2 });

        Assert.False(success);
    }

    [Fact]
    public async Task Iniciar_VehiculoNoPerteneceAlUsuario_Falla()
    {
        _usuarioRepo.Setup(r => r.GetById(1)).ReturnsAsync(new Usuario { Id = 1, Correo = "a@a.com" });
        _vehiculoRepo.Setup(r => r.GetById(2)).ReturnsAsync(new Vehiculo { Id = 2, UsuarioId = 999 });

        var (success, _, _) = await _sut.Iniciar(new ViajeRequestDto { UsuarioId = 1, VehiculoId = 2 });

        Assert.False(success);
    }

    [Fact]
    public async Task Iniciar_Valido_CreaViaje()
    {
        _usuarioRepo.Setup(r => r.GetById(1)).ReturnsAsync(new Usuario { Id = 1, Correo = "a@a.com" });
        _vehiculoRepo.Setup(r => r.GetById(2)).ReturnsAsync(new Vehiculo { Id = 2, UsuarioId = 1 });
        _viajeRepo.Setup(r => r.GetEnCursoByUsuario(1)).ReturnsAsync(new List<Viaje>());

        var (success, _, _) = await _sut.Iniciar(new ViajeRequestDto { UsuarioId = 1, VehiculoId = 2 });

        Assert.True(success);
        _viajeRepo.Verify(r => r.Add(It.IsAny<Viaje>()), Times.Once);
    }

    [Fact]
    public async Task Iniciar_ConViajeAbiertoDeHoy_LoRetomaSinCrearOtro()
    {
        _usuarioRepo.Setup(r => r.GetById(1)).ReturnsAsync(new Usuario { Id = 1, Correo = "a@a.com" });
        _vehiculoRepo.Setup(r => r.GetById(2)).ReturnsAsync(new Vehiculo { Id = 2, UsuarioId = 1 });
        // Ya hay un viaje abierto de hoy (id 5).
        _viajeRepo.Setup(r => r.GetEnCursoByUsuario(1)).ReturnsAsync(new List<Viaje>
        {
            new() { Id = 5, UsuarioId = 1, Estado = "En Curso", FechaInicio = DateTime.UtcNow }
        });

        var (success, _, id) = await _sut.Iniciar(new ViajeRequestDto { UsuarioId = 1, VehiculoId = 2 });

        Assert.True(success);
        Assert.Equal(5, id);                                   // retoma el existente
        _viajeRepo.Verify(r => r.Add(It.IsAny<Viaje>()), Times.Never); // no crea otro
    }

    [Fact]
    public async Task Iniciar_ConViajeAbiertoDeAyer_LoCierraYCreaNuevo()
    {
        _usuarioRepo.Setup(r => r.GetById(1)).ReturnsAsync(new Usuario { Id = 1, Correo = "a@a.com" });
        _vehiculoRepo.Setup(r => r.GetById(2)).ReturnsAsync(new Vehiculo { Id = 2, UsuarioId = 1 });
        var viejo = new Viaje { Id = 9, UsuarioId = 1, Estado = "En Curso", FechaInicio = DateTime.UtcNow.AddDays(-1) };
        _viajeRepo.Setup(r => r.GetEnCursoByUsuario(1)).ReturnsAsync(new List<Viaje> { viejo });
        _eventoRepo.Setup(r => r.GetByViajeId(9)).ReturnsAsync(new List<Evento>());

        var (success, _, _) = await _sut.Iniciar(new ViajeRequestDto { UsuarioId = 1, VehiculoId = 2 });

        Assert.True(success);
        Assert.Equal("Finalizado", viejo.Estado);             // el de ayer se cerró
        _viajeRepo.Verify(r => r.Update(It.Is<Viaje>(v => v.Id == 9 && v.Estado == "Finalizado")), Times.Once);
        _viajeRepo.Verify(r => r.Add(It.IsAny<Viaje>()), Times.Once); // y se creó uno nuevo
    }

    [Fact]
    public async Task Finalizar_ViajeNoEnCurso_Falla()
    {
        _viajeRepo.Setup(r => r.GetById(1)).ReturnsAsync(new Viaje { Id = 1, Estado = "Finalizado" });

        var (success, _) = await _sut.Finalizar(1, new FinalizarViajeRequestDto { PuntajeConduccion = 80 });

        Assert.False(success);
    }

    [Fact]
    public async Task Finalizar_Valido_CierraConNetoDeEventos()
    {
        // El viaje es la misma instancia que mutará el servicio, así podemos verificar el resultado.
        var viaje = new Viaje { Id = 1, UsuarioId = 7, Estado = "En Curso" };
        _viajeRepo.Setup(r => r.GetById(1)).ReturnsAsync(viaje);
        _eventoRepo.Setup(r => r.GetByViajeId(1)).ReturnsAsync(new List<Evento>
        {
            new() { ImpactoPuntaje = 10 },
            new() { ImpactoPuntaje = -5 }
        });

        // DuracionMinutos ≥ 5 para superar el mínimo (viajes más cortos se cancelan solos).
        var (success, _) = await _sut.Finalizar(1, new FinalizarViajeRequestDto { PuntajeConduccion = 80, DuracionMinutos = 30 });

        Assert.True(success);
        Assert.Equal("Finalizado", viaje.Estado);
        Assert.Equal(5, viaje.PuntosGanados);   // 10 + (-5)
    }

    [Fact]
    public async Task Cancelar_ViajeEnCurso_MarcaCancelado()
    {
        var viaje = new Viaje { Id = 1, Estado = "En Curso" };
        _viajeRepo.Setup(r => r.GetById(1)).ReturnsAsync(viaje);

        var (success, _) = await _sut.Cancelar(1);

        Assert.True(success);
        Assert.Equal("Cancelado", viaje.Estado);
    }
}
