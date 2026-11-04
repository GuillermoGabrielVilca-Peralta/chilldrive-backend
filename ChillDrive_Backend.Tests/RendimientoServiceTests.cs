using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Repositories;
using ChillDrive_Backend.Services.Implements;
using Moq;
using Xunit;

namespace ChillDrive_Backend.Tests;

// Prueba los indicadores de rendimiento de la flota del panel de administración.
public class RendimientoServiceTests
{
    private readonly Mock<IViajeRepository> _viajeRepo = new();
    private readonly RendimientoService      _sut;
    private int _siguienteId = 1;

    public RendimientoServiceTests()
    {
        _sut = new RendimientoService(_viajeRepo.Object);
    }

    private static Evento E(string tipo, decimal? lat = null, decimal? lon = null) => new()
    {
        TipoEvento     = tipo,
        ImpactoPuntaje = ImpactoEventos.Para(tipo),
        Latitud        = lat,
        Longitud       = lon,
        FechaEvento    = DateTime.UtcNow
    };

    private Viaje V(int usuarioId, int haceDias, decimal km, decimal puntaje, params Evento[] eventos) => new()
    {
        Id                = _siguienteId++,
        UsuarioId         = usuarioId,
        Estado            = "Finalizado",
        FechaInicio       = DateTime.UtcNow.AddDays(-haceDias).AddHours(-1),
        DistanciaKm       = km,
        DuracionMinutos   = 30,
        PuntajeConduccion = puntaje,
        Usuario           = new Usuario  { Id = usuarioId, Nombres = $"C{usuarioId}", Apellidos = "Demo", Correo = "c@c.com" },
        Vehiculo          = new Vehiculo { Placa = $"ABC-{usuarioId}" },
        Eventos           = eventos.ToList()
    };

    private void Viajes(params Viaje[] viajes) =>
        _viajeRepo.Setup(r => r.GetFinalizadosDesde(It.IsAny<DateTime>())).ReturnsAsync(viajes.ToList());

    [Theory]
    [InlineData(15, 40, 16)]
    [InlineData(30, 2, 16)]
    [InlineData(30, 40, 0)]
    public async Task Get_RechazaParametrosFueraDeRango(int dias, double kmPorGalon, double precio)
    {
        var (success, _, data) = await _sut.Get(dias, kmPorGalon, precio);

        Assert.False(success);
        Assert.Null(data);
        _viajeRepo.Verify(r => r.GetFinalizadosDesde(It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task Get_ConsultaElPeriodoActualYElAnterior()
    {
        DateTime? desde = null;
        _viajeRepo.Setup(r => r.GetFinalizadosDesde(It.IsAny<DateTime>()))
                  .Callback<DateTime>(d => desde = d)
                  .ReturnsAsync(new List<Viaje>());

        await _sut.Get(30, 40, 16);

        Assert.True(Math.Abs((desde!.Value - DateTime.UtcNow.AddDays(-60)).TotalSeconds) < 5);
    }

    [Fact]
    public async Task Get_SeparaLosPeriodosYCalculaElResumen()
    {
        Viajes(
            V(1, 1, 50, 90, E("Exceso Velocidad"), E("Señal Respetada")),
            V(2, 2, 50, 70, E("Frenado Brusco")),
            V(1, 40, 100, 60));   // periodo anterior

        var (_, _, r) = await _sut.Get(30, 40, 16);

        Assert.Equal(2,   r!.Actual.Viajes);
        Assert.Equal(2,   r.Actual.Conductores);
        Assert.Equal(100, r.Actual.Km);
        Assert.Equal(80,  r.Actual.PuntajePromedio);
        Assert.Equal(2,   r.Actual.Infracciones);
        Assert.Equal(2,   r.Actual.InfraccionesPor100Km);
        Assert.Equal(50,  r.Actual.PorcentajeViajesSeguros);
        Assert.Equal(1,   r.Anterior.Viajes);
        Assert.Equal(60,  r.Anterior.PuntajePromedio);
    }

    [Fact]
    public async Task Get_SinViajesDevuelvePromediosVacios()
    {
        Viajes();

        var (success, _, r) = await _sut.Get(7, 40, 16);

        Assert.True(success);
        Assert.Null(r!.Actual.PuntajePromedio);
        Assert.Null(r.Actual.PorcentajeViajesSeguros);
        Assert.Equal(0, r.Actual.InfraccionesPor100Km);
        Assert.Equal(100, r.Actual.IndiceEficiencia);
        Assert.Empty(r.Conductores);
    }

    [Fact]
    public async Task Get_EstimaCombustibleDesperdiciadoYAhorro()
    {
        // 100 km con 10 maniobras → 10/100 km × 0,5 % = 5 % de sobreconsumo.
        var maniobras = Enumerable.Range(0, 10).Select(_ => E("Aceleración Brusca")).ToArray();
        // Periodo anterior: 20 maniobras en 100 km → 10 %.
        var anteriores = Enumerable.Range(0, 20).Select(_ => E("Frenado Brusco")).ToArray();
        Viajes(V(1, 1, 100, 80, maniobras), V(1, 40, 100, 70, anteriores));

        var (_, _, r) = await _sut.Get(30, 40, 16);

        // Base: 100 km / 40 km/gal = 2,5 gal.
        Assert.Equal(5,     r!.Actual.SobreconsumoPorcentaje);
        Assert.Equal(0.1,   r.Combustible.GalonesDesperdiciados);      // 2,5 × 5 %
        Assert.Equal(2,     r.Combustible.SolesDesperdiciados);        // 0,125 gal × S/ 16
        Assert.Equal(0.1,   r.Combustible.GalonesAhorrados);           // 2,5 × (10 % − 5 %)
        Assert.Equal(2,     r.Combustible.SolesAhorrados);
        Assert.Equal(83,    r.Actual.IndiceEficiencia);                // 100 × (1 − 5/30)
    }

    [Fact]
    public void Sobreconsumo_SeTopaEnElMaximo()
    {
        Assert.Equal(EficienciaCombustible.SobreconsumoMaximo, EficienciaCombustible.Sobreconsumo(500, 10));
        Assert.Equal(0, EficienciaCombustible.Indice(EficienciaCombustible.SobreconsumoMaximo));
        Assert.Equal(0, EficienciaCombustible.Sobreconsumo(5, 0));
    }

    [Fact]
    public async Task Get_AgrupaInfraccionesEnZonasCriticas()
    {
        Viajes(V(1, 1, 10, 50,
            E("Exceso Velocidad", -16.40011m, -71.53011m),
            E("Exceso Velocidad", -16.40012m, -71.53012m),
            E("Frenado Brusco",   -16.40013m, -71.53013m),
            E("Frenado Brusco",   -16.45000m, -71.50000m),
            E("Señal Respetada",  -16.40011m, -71.53011m)));   // no es infracción

        var (_, _, r) = await _sut.Get(30, 40, 16);

        Assert.Equal(2, r!.ZonasCriticas.Count);
        var principal = r.ZonasCriticas[0];
        Assert.Equal(3, principal.Infracciones);
        Assert.Equal("Exceso Velocidad", principal.TipoPrincipal);
    }

    [Fact]
    public async Task Get_MarcaConductoresQueRequierenCapacitacion()
    {
        Viajes(
            V(1, 1, 100, 95),
            V(2, 1, 100, 55),
            V(3, 1, 100, 75), V(3, 40, 100, 85));

        var (_, _, r) = await _sut.Get(30, 40, 16);

        var porId = r!.Conductores.ToDictionary(c => c.UsuarioId);
        Assert.Null(porId[1].MotivoCapacitacion);
        Assert.Equal("Puntaje bajo (55)", porId[2].MotivoCapacitacion);
        Assert.Equal("Bajó 10 pts",        porId[3].MotivoCapacitacion);
        Assert.Equal(85, porId[3].PuntajeAnterior);
        // Ordenados del mejor al peor puntaje.
        Assert.Equal([1, 3, 2], r.Conductores.Select(c => c.UsuarioId));
    }

    [Fact]
    public async Task Get_CuentaEventosPorTipoYHoraDePeru()
    {
        var evento = E("Uso Celular");
        evento.FechaEvento = new DateTime(2026, 11, 3, 23, 30, 0, DateTimeKind.Utc);   // 18:30 en Perú
        Viajes(V(1, 1, 10, 80, evento, E("Señal Respetada")));

        var (_, _, r) = await _sut.Get(30, 40, 16);

        Assert.Equal(24, r!.PorHora.Count);
        Assert.Equal(1, r.PorHora[18].Infracciones);
        var celular = r.PorTipo.Single(t => t.Tipo == "Uso Celular");
        Assert.True(celular.EsInfraccion);
        Assert.False(r.PorTipo.Single(t => t.Tipo == "Señal Respetada").EsInfraccion);
    }
}
