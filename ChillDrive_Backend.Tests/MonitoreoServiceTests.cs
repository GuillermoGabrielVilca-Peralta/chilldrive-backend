using ChillDrive_Backend.Models;
using ChillDrive_Backend.Repositories;
using ChillDrive_Backend.Services.Implements;
using Moq;
using Xunit;

namespace ChillDrive_Backend.Tests;

// Prueba el armado de los vehículos en ruta para el mapa del panel de administración.
public class MonitoreoServiceTests
{
    private readonly Mock<IViajeRepository> _viajeRepo = new();
    private readonly MonitoreoService        _sut;

    public MonitoreoServiceTests()
    {
        _sut = new MonitoreoService(_viajeRepo.Object);
    }

    private static Viaje ViajeActivo(DateTime ultimaUbicacion, params Evento[] eventos) => new()
    {
        Id                   = 5,
        UsuarioId            = 7,
        Estado               = "En Curso",
        FechaInicio          = DateTime.UtcNow.AddMinutes(-20),
        UltimaLatitud        = -12.05m,
        UltimaLongitud       = -77.04m,
        UltimaVelocidadKmh   = 35m,
        UltimaUbicacionFecha = ultimaUbicacion,
        Usuario              = new Usuario  { Id = 7, Nombres = "Ana", Apellidos = "Torres", Correo = "a@a.com" },
        Vehiculo             = new Vehiculo { Id = 3, Placa = "ABC-123", Marca = "Toyota", Modelo = "Yaris" },
        Eventos              = eventos.ToList()
    };

    [Fact]
    public async Task GetViajesActivos_ConsultaSoloLaVentanaDeActividad()
    {
        DateTime? desdeConsultado = null;
        _viajeRepo.Setup(r => r.GetActivosConUbicacion(It.IsAny<DateTime>()))
                  .Callback<DateTime>(d => desdeConsultado = d)
                  .ReturnsAsync(new List<Viaje>());

        await _sut.GetViajesActivos();

        // Pide los viajes que transmitieron en los últimos VentanaActividadMinutos.
        var esperado = DateTime.UtcNow.AddMinutes(-MonitoreoService.VentanaActividadMinutos);
        Assert.NotNull(desdeConsultado);
        Assert.True(Math.Abs((desdeConsultado!.Value - esperado).TotalSeconds) < 5);
    }

    [Fact]
    public async Task GetViajesActivos_MapeaConductorVehiculoYPosicion()
    {
        _viajeRepo.Setup(r => r.GetActivosConUbicacion(It.IsAny<DateTime>()))
                  .ReturnsAsync(new List<Viaje> { ViajeActivo(DateTime.UtcNow.AddSeconds(-30)) });

        var resultado = Assert.Single(await _sut.GetViajesActivos());

        Assert.Equal("Ana Torres", resultado.Conductor);
        Assert.Equal("ABC-123",    resultado.Placa);
        Assert.Equal("Toyota Yaris", resultado.Vehiculo);
        Assert.Equal(-12.05m,      resultado.Latitud);
        Assert.Equal(-77.04m,      resultado.Longitud);
        Assert.InRange(resultado.SegundosDesdeUltimaUbicacion, 29, 35);
    }

    [Fact]
    public async Task GetViajesActivos_SinEventos_PuntajePerfecto()
    {
        _viajeRepo.Setup(r => r.GetActivosConUbicacion(It.IsAny<DateTime>()))
                  .ReturnsAsync(new List<Viaje> { ViajeActivo(DateTime.UtcNow) });

        var resultado = Assert.Single(await _sut.GetViajesActivos());

        Assert.Equal(100, resultado.PuntajeActual);
        Assert.Empty(resultado.Eventos);
    }

    [Fact]
    public async Task GetViajesActivos_CalculaPuntajeYSoloDibujaEventosConCoordenadas()
    {
        var conUbicacion = new Evento
        {
            TipoEvento = "Frenado Brusco", ImpactoPuntaje = -10,
            Latitud = -12.06m, Longitud = -77.03m, FechaEvento = DateTime.UtcNow.AddMinutes(-5)
        };
        var sinUbicacion = new Evento
        {
            TipoEvento = "Exceso Velocidad", ImpactoPuntaje = -20, FechaEvento = DateTime.UtcNow.AddMinutes(-3)
        };
        _viajeRepo.Setup(r => r.GetActivosConUbicacion(It.IsAny<DateTime>()))
                  .ReturnsAsync(new List<Viaje> { ViajeActivo(DateTime.UtcNow, conUbicacion, sinUbicacion) });

        var resultado = Assert.Single(await _sut.GetViajesActivos());

        // Ambos eventos cuentan para el puntaje (frenado 90, velocidad 80, resto 100 → 94)…
        Assert.Equal(94, resultado.PuntajeActual);
        // …pero en el mapa solo se dibuja el que trae coordenadas.
        var evento = Assert.Single(resultado.Eventos);
        Assert.Equal("Frenado Brusco", evento.TipoEvento);
    }
}
