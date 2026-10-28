using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Repositories;
using ChillDrive_Backend.Services.Implements;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ChillDrive_Backend.Tests;

// Prueba el ciclo de la suscripción: prueba gratis, pago con puntos, pago con Yape/efectivo
// (pendiente hasta que el admin aprueba), rechazo, cancelación y vencimiento automático.
public class SuscripcionServiceTests
{
    private readonly Mock<ISuscripcionRepository>     _repo           = new();
    private readonly Mock<IUsuarioRepository>         _usuarioRepo    = new();
    private readonly Mock<IPuntosUsuarioRepository>   _puntosRepo     = new();
    private readonly Mock<IMovimientoPuntoRepository> _movimientoRepo = new();
    private readonly SuscripcionService               _sut;
    private readonly Usuario                          _usuario = new() { Id = 1, Correo = "a@a.com", TipoPlan = "Gratuito" };

    private static DateOnly Hoy => FechaPeru.Hoy;

    public SuscripcionServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Pagos:YapeNumero"]  = "937395614",
                ["Pagos:YapeTitular"] = "ChillDrive"
            })
            .Build();

        _sut = new SuscripcionService(_repo.Object, _usuarioRepo.Object, _puntosRepo.Object,
                                      _movimientoRepo.Object, new FakeUnitOfWork(), config);

        _usuarioRepo.Setup(r => r.GetById(1)).ReturnsAsync(_usuario);
        _repo.Setup(r => r.Add(It.IsAny<Suscripcione>())).ReturnsAsync(10);
    }

    private static SolicitarSuscripcionRequestDto Pedido(string plan, string? metodo = null, string? operacion = null) =>
        new() { Plan = plan, MetodoPago = metodo, NumeroOperacion = operacion };

    // ── Solicitar: validaciones generales ────────────────────────────────────

    [Fact]
    public async Task Solicitar_PlanInvalido_Falla()
    {
        var (success, _, _) = await _sut.Solicitar(1, Pedido("Vitalicio"));

        Assert.False(success);
        _repo.Verify(r => r.Add(It.IsAny<Suscripcione>()), Times.Never);
    }

    [Fact]
    public async Task Solicitar_ConPagoPendiente_Falla()
    {
        _repo.Setup(r => r.GetPendienteByUsuarioId(1)).ReturnsAsync(new Suscripcione { Id = 3, Estado = "Pendiente" });

        var (success, _, _) = await _sut.Solicitar(1, Pedido("Mensual", "Yape", "12345678"));

        Assert.False(success);
        _repo.Verify(r => r.Add(It.IsAny<Suscripcione>()), Times.Never);
    }

    // ── Prueba gratis ────────────────────────────────────────────────────────

    [Fact]
    public async Task Solicitar_Prueba_PrimeraVez_ActivaAlInstante()
    {
        Suscripcione? creada = null;
        _repo.Setup(r => r.Add(It.IsAny<Suscripcione>())).Callback<Suscripcione>(s => creada = s).ReturnsAsync(10);

        var (success, _, _) = await _sut.Solicitar(1, Pedido("Prueba"));

        Assert.True(success);
        Assert.Equal("Activa", creada!.Estado);
        Assert.Equal(0m, creada.MontoSoles);
        Assert.Equal(Hoy.AddDays(30), creada.FechaFin);
        Assert.Equal("Profesional", _usuario.TipoPlan);
    }

    [Fact]
    public async Task Solicitar_Prueba_YaUsada_Falla()
    {
        _repo.Setup(r => r.UsoPrueba(1)).ReturnsAsync(true);

        var (success, _, _) = await _sut.Solicitar(1, Pedido("Prueba"));

        Assert.False(success);
        Assert.Equal("Gratuito", _usuario.TipoPlan);
    }

    // ── Pago con puntos ──────────────────────────────────────────────────────

    [Fact]
    public async Task Solicitar_Puntos_Insuficientes_Falla()
    {
        _puntosRepo.Setup(r => r.GetByUsuarioId(1)).ReturnsAsync(new PuntosUsuario { PuntosActuales = 100 });

        var (success, _, _) = await _sut.Solicitar(1, Pedido("Puntos"));

        Assert.False(success);
        _puntosRepo.Verify(r => r.DescontarPuntos(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Solicitar_Puntos_Suficientes_DescuentaRegistraLedgerYActiva()
    {
        _puntosRepo.Setup(r => r.GetByUsuarioId(1)).ReturnsAsync(new PuntosUsuario { PuntosActuales = 2000 });

        var (success, _, _) = await _sut.Solicitar(1, Pedido("Puntos"));

        Assert.True(success);
        _puntosRepo.Verify(r => r.DescontarPuntos(1, PlanesSuscripcion.PuntosPorMes), Times.Once);
        _movimientoRepo.Verify(r => r.Add(It.Is<MovimientosPunto>(m =>
            m.Tipo == "Canjeado" && m.Puntos == -PlanesSuscripcion.PuntosPorMes)), Times.Once);
        Assert.Equal("Profesional", _usuario.TipoPlan);
    }

    // ── Pago con dinero: queda pendiente, NO activa el plan ──────────────────

    [Fact]
    public async Task Solicitar_Yape_SinNumeroOperacion_Falla()
    {
        var (success, _, _) = await _sut.Solicitar(1, Pedido("Mensual", "Yape"));

        Assert.False(success);
    }

    [Fact]
    public async Task Solicitar_Yape_NumeroOperacionRepetido_Falla()
    {
        _repo.Setup(r => r.ExisteReferenciaYape("12345678")).ReturnsAsync(true);

        var (success, _, _) = await _sut.Solicitar(1, Pedido("Mensual", "Yape", "12345678"));

        Assert.False(success);
    }

    [Fact]
    public async Task Solicitar_Yape_Valido_QuedaPendienteSinActivarPlan()
    {
        Suscripcione? creada = null;
        _repo.Setup(r => r.Add(It.IsAny<Suscripcione>())).Callback<Suscripcione>(s => creada = s).ReturnsAsync(10);

        var (success, _, id) = await _sut.Solicitar(1, Pedido("Trimestral", "yape", "12345678"));

        Assert.True(success);
        Assert.Equal(10, id);
        Assert.Equal("Pendiente", creada!.Estado);
        Assert.Equal("Yape", creada.MetodoPago);
        Assert.Equal("12345678", creada.ReferenciaPago);
        // El precio y los días los pone el servidor según el plan (paga 3, recibe 4).
        Assert.Equal(45m, creada.MontoSoles);
        Assert.Equal(120, creada.Dias);
        // Sin aprobación del admin, el conductor sigue en Gratuito.
        Assert.Equal("Gratuito", _usuario.TipoPlan);
    }

    [Fact]
    public async Task Solicitar_Efectivo_GeneraCodigoDePago()
    {
        Suscripcione? creada = null;
        _repo.Setup(r => r.Add(It.IsAny<Suscripcione>())).Callback<Suscripcione>(s => creada = s).ReturnsAsync(10);

        var (success, mensaje, _) = await _sut.Solicitar(1, Pedido("Mensual", "Efectivo"));

        Assert.True(success);
        Assert.Equal("Pendiente", creada!.Estado);
        Assert.Matches("^CD-[A-Z2-9]{6}$", creada.CodigoPago!);
        Assert.Contains(creada.CodigoPago!, mensaje);
    }

    [Fact]
    public async Task Solicitar_MetodoNoPermitido_Falla()
    {
        var (success, _, _) = await _sut.Solicitar(1, Pedido("Mensual", "Tarjeta"));

        Assert.False(success);
    }

    // ── Aprobar / rechazar (administrador) ───────────────────────────────────

    [Fact]
    public async Task Aprobar_Pendiente_ActivaDesdeHoyYEleva()
    {
        var pendiente = new Suscripcione { Id = 5, UsuarioId = 1, Estado = "Pendiente", Dias = 30 };
        _repo.Setup(r => r.GetById(5)).ReturnsAsync(pendiente);

        var (success, _) = await _sut.Aprobar(5);

        Assert.True(success);
        Assert.Equal("Activa", pendiente.Estado);
        Assert.Equal(Hoy, pendiente.FechaInicio);
        Assert.Equal(Hoy.AddDays(30), pendiente.FechaFin);
        Assert.NotNull(pendiente.FechaAprobacion);
        Assert.Equal("Profesional", _usuario.TipoPlan);
    }

    [Fact]
    public async Task Aprobar_ConPlanVigente_EncadenaSinPerderDias()
    {
        var pendiente = new Suscripcione { Id = 5, UsuarioId = 1, Estado = "Pendiente", Dias = 30 };
        _repo.Setup(r => r.GetById(5)).ReturnsAsync(pendiente);
        // Aún le quedan 10 días del plan actual.
        _repo.Setup(r => r.GetActivaByUsuarioId(1))
             .ReturnsAsync(new Suscripcione { Estado = "Activa", FechaFin = Hoy.AddDays(10) });

        var (success, _) = await _sut.Aprobar(5);

        Assert.True(success);
        Assert.Equal(Hoy.AddDays(10), pendiente.FechaInicio);
        Assert.Equal(Hoy.AddDays(40), pendiente.FechaFin);
    }

    [Fact]
    public async Task Aprobar_NoPendiente_Falla()
    {
        _repo.Setup(r => r.GetById(5)).ReturnsAsync(new Suscripcione { Id = 5, UsuarioId = 1, Estado = "Rechazada" });

        var (success, _) = await _sut.Aprobar(5);

        Assert.False(success);
        Assert.Equal("Gratuito", _usuario.TipoPlan);
    }

    [Fact]
    public async Task Rechazar_Pendiente_GuardaMotivoSinActivar()
    {
        var pendiente = new Suscripcione { Id = 5, UsuarioId = 1, Estado = "Pendiente" };
        _repo.Setup(r => r.GetById(5)).ReturnsAsync(pendiente);

        var (success, _) = await _sut.Rechazar(5, "  No encontramos el pago  ");

        Assert.True(success);
        Assert.Equal("Rechazada", pendiente.Estado);
        Assert.Equal("No encontramos el pago", pendiente.MotivoRechazo);
        Assert.Equal("Gratuito", _usuario.TipoPlan);
    }

    // ── Cancelar (conductor) ─────────────────────────────────────────────────

    [Fact]
    public async Task Cancelar_Pendiente_LaRetira()
    {
        var pendiente = new Suscripcione { Id = 5, UsuarioId = 1, Estado = "Pendiente" };
        _repo.Setup(r => r.GetById(5)).ReturnsAsync(pendiente);

        var (success, _) = await _sut.Cancelar(5);

        Assert.True(success);
        Assert.Equal("Cancelada", pendiente.Estado);
    }

    [Fact]
    public async Task Cancelar_Activa_NoSePermiteYConservaDiasPagados()
    {
        var activa = new Suscripcione { Id = 5, UsuarioId = 1, Estado = "Activa", FechaFin = Hoy.AddDays(20) };
        _repo.Setup(r => r.GetById(5)).ReturnsAsync(activa);

        var (success, _) = await _sut.Cancelar(5);

        Assert.False(success);
        Assert.Equal("Activa", activa.Estado);
    }

    // ── Vencimientos ─────────────────────────────────────────────────────────

    [Fact]
    public async Task VencerSuscripciones_SinOtraVigente_VuelveAGratuito()
    {
        _usuario.TipoPlan = "Profesional";
        var vencida = new Suscripcione { Id = 5, UsuarioId = 1, Estado = "Activa", FechaFin = Hoy };
        _repo.Setup(r => r.GetActivasVencidas(Hoy)).ReturnsAsync(new List<Suscripcione> { vencida });
        _repo.Setup(r => r.TieneVigente(1, Hoy)).ReturnsAsync(false);

        var cantidad = await _sut.VencerSuscripciones();

        Assert.Equal(1, cantidad);
        Assert.Equal("Vencida", vencida.Estado);
        Assert.Equal("Gratuito", _usuario.TipoPlan);
    }

    [Fact]
    public async Task VencerSuscripciones_ConRenovacionVigente_SigueProfesional()
    {
        _usuario.TipoPlan = "Profesional";
        var vencida = new Suscripcione { Id = 5, UsuarioId = 1, Estado = "Activa", FechaFin = Hoy };
        _repo.Setup(r => r.GetActivasVencidas(Hoy)).ReturnsAsync(new List<Suscripcione> { vencida });
        _repo.Setup(r => r.TieneVigente(1, Hoy)).ReturnsAsync(true);

        await _sut.VencerSuscripciones();

        Assert.Equal("Vencida", vencida.Estado);
        Assert.Equal("Profesional", _usuario.TipoPlan);
    }

    // ── Estado ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetEstado_PlanQueVenceEn3Dias_MarcaPorVencer()
    {
        _usuario.TipoPlan = "Profesional";
        _repo.Setup(r => r.GetActivasVencidas(It.IsAny<DateOnly>())).ReturnsAsync(new List<Suscripcione>());
        _repo.Setup(r => r.GetByUsuarioId(1)).ReturnsAsync(new List<Suscripcione>
        {
            new() { Id = 5, UsuarioId = 1, Estado = "Activa", Plan = "Mensual", MetodoPago = "Yape",
                    FechaInicio = Hoy.AddDays(-27), FechaFin = Hoy.AddDays(3) }
        });

        var estado = await _sut.GetEstado(1);

        Assert.NotNull(estado);
        Assert.Equal(3, estado!.DiasRestantes);
        Assert.True(estado.PorVencer);
        Assert.Null(estado.Pendiente);
    }

    [Fact]
    public async Task GetPlanes_DisponibilidadSegunHistorialYPuntos()
    {
        _repo.Setup(r => r.UsoPrueba(1)).ReturnsAsync(true);
        _puntosRepo.Setup(r => r.GetByUsuarioId(1)).ReturnsAsync(new PuntosUsuario { PuntosActuales = 500 });

        var planes = await _sut.GetPlanes(1);

        Assert.False(planes.Planes.Single(p => p.Codigo == "Prueba").Disponible);
        Assert.False(planes.Planes.Single(p => p.Codigo == "Puntos").Disponible);
        Assert.True(planes.Planes.Single(p => p.Codigo == "Mensual").Disponible);
        Assert.Equal("937395614", planes.YapeNumero);
        Assert.Equal(500, planes.PuntosActuales);
    }
}
