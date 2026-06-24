using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Repositories;
using ChillDrive_Backend.Services.Implements;
using Moq;
using Xunit;

namespace ChillDrive_Backend.Tests;

// Prueba que registrar un evento ajuste los puntos del conductor según el tipo de señal.
public class EventoServiceTests
{
    private const int UsuarioActual = 7;

    private readonly Mock<IEventoRepository>          _eventoRepo     = new();
    private readonly Mock<IViajeRepository>           _viajeRepo      = new();
    private readonly Mock<ICamaraRepository>          _camaraRepo     = new();
    private readonly Mock<IPuntosUsuarioRepository>   _puntosRepo     = new();
    private readonly Mock<IMovimientoPuntoRepository> _movimientoRepo = new();
    private readonly EventoService                    _sut;

    public EventoServiceTests()
    {
        _sut = new EventoService(
            _eventoRepo.Object, _viajeRepo.Object, _camaraRepo.Object,
            _puntosRepo.Object, _movimientoRepo.Object, new FakeUnitOfWork());
    }

    private void ConCamara(int id = 1) =>
        _camaraRepo.Setup(r => r.GetById(id)).ReturnsAsync(new Camara { Id = id, VehiculoId = 1 });

    private void ConViajeEnCurso(int viajeId = 1, int usuarioId = UsuarioActual) =>
        _viajeRepo.Setup(r => r.GetById(viajeId))
                  .ReturnsAsync(new Viaje { Id = viajeId, UsuarioId = usuarioId, Estado = "En Curso" });

    [Fact]
    public async Task Registrar_SenalRespetada_SumaPuntosYRegistraGanado()
    {
        ConCamara();
        ConViajeEnCurso(viajeId: 1, usuarioId: 7);

        var (success, _, _, _) = await _sut.Registrar(new EventoRequestDto
        {
            CamaraId = 1, ViajeId = 1, TipoEvento = "Señal Respetada"
        }, UsuarioActual);

        Assert.True(success);
        // +10 al conductor 7, registrado como "Ganado".
        _puntosRepo.Verify(r => r.AplicarImpactoEvento(7, 10), Times.Once);
        _movimientoRepo.Verify(r => r.Add(It.Is<MovimientosPunto>(
            m => m.Tipo == MovimientoTipos.Ganado && m.Puntos == 10 && m.UsuarioId == 7)), Times.Once);
    }

    [Fact]
    public async Task Registrar_SenalIgnorada_RestaPuntosYRegistraPenalizado()
    {
        ConCamara();
        ConViajeEnCurso(viajeId: 1, usuarioId: 7);

        var (success, _, _, _) = await _sut.Registrar(new EventoRequestDto
        {
            CamaraId = 1, ViajeId = 1, TipoEvento = "Señal Ignorada"
        }, UsuarioActual);

        Assert.True(success);
        // -20 al conductor 7, registrado como "Ajuste".
        _puntosRepo.Verify(r => r.AplicarImpactoEvento(7, -20), Times.Once);
        _movimientoRepo.Verify(r => r.Add(It.Is<MovimientosPunto>(
            m => m.Tipo == MovimientoTipos.Ajuste && m.Puntos == -20)), Times.Once);
    }

    [Fact]
    public async Task Registrar_SinViaje_GuardaEventoPeroNoTocaPuntos()
    {
        ConCamara();

        var (success, _, _, _) = await _sut.Registrar(new EventoRequestDto
        {
            CamaraId = 1, ViajeId = null, TipoEvento = "Señal Respetada"
        }, UsuarioActual);

        Assert.True(success);
        _puntosRepo.Verify(r => r.AplicarImpactoEvento(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Registrar_TipoDeEventoInvalido_Falla()
    {
        var (success, _, _, _) = await _sut.Registrar(new EventoRequestDto
        {
            CamaraId = 1, TipoEvento = "NoExiste"
        }, UsuarioActual);

        Assert.False(success);
    }

    // ── Fase C: idempotencia ────────────────────────────────────────────────

    [Fact]
    public async Task Registrar_IdLocalYaExiste_NoDuplicaNiTocaPuntos()
    {
        // El mismo evento ya fue registrado antes (reenvío de la cola offline).
        _eventoRepo.Setup(r => r.GetByClienteEventoId("abc-123"))
                   .ReturnsAsync(new Evento { Id = 99, ClienteEventoId = "abc-123" });

        var (success, _, id, duplicado) = await _sut.Registrar(new EventoRequestDto
        {
            CamaraId = 1, ViajeId = 1, TipoEvento = "Señal Ignorada", IdLocal = "abc-123"
        }, UsuarioActual);

        Assert.True(success);
        Assert.True(duplicado);
        Assert.Equal(99, id);
        // No se vuelve a insertar ni a ajustar puntos.
        _eventoRepo.Verify(r => r.Add(It.IsAny<Evento>()), Times.Never);
        _puntosRepo.Verify(r => r.AplicarImpactoEvento(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Registrar_IdLocalNuevo_RegistraNormalmente()
    {
        ConCamara();
        ConViajeEnCurso(viajeId: 1, usuarioId: 7);
        _eventoRepo.Setup(r => r.GetByClienteEventoId("nuevo-1")).ReturnsAsync((Evento?)null);

        var (success, _, _, duplicado) = await _sut.Registrar(new EventoRequestDto
        {
            CamaraId = 1, ViajeId = 1, TipoEvento = "Señal Respetada", IdLocal = "nuevo-1"
        }, UsuarioActual);

        Assert.True(success);
        Assert.False(duplicado);
        _eventoRepo.Verify(r => r.Add(It.IsAny<Evento>()), Times.Once);
    }

    // ── Fase C: pertenencia ─────────────────────────────────────────────────

    [Fact]
    public async Task Registrar_ViajeDeOtroUsuario_Falla()
    {
        ConCamara();
        ConViajeEnCurso(viajeId: 1, usuarioId: 7); // el viaje es del usuario 7

        // …pero quien envía es el usuario 99.
        var (success, mensaje, _, _) = await _sut.Registrar(new EventoRequestDto
        {
            CamaraId = 1, ViajeId = 1, TipoEvento = "Señal Ignorada"
        }, usuarioActualId: 99);

        Assert.False(success);
        Assert.Contains("no pertenece", mensaje);
        _puntosRepo.Verify(r => r.AplicarImpactoEvento(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    // ── Fase C.2: anti-flood + hora de detección ────────────────────────────

    [Fact]
    public async Task Registrar_MismoTipoMuySeguido_SeDescarta()
    {
        ConCamara();
        ConViajeEnCurso(viajeId: 1, usuarioId: 7);
        // Ya hay un "Frenado Brusco" en la ventana → flood.
        _eventoRepo.Setup(r => r.ExisteEventoTipoEnVentana(
                       1, "Frenado Brusco", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                   .ReturnsAsync(true);

        var (success, mensaje, _, _) = await _sut.Registrar(new EventoRequestDto
        {
            CamaraId = 1, ViajeId = 1, TipoEvento = "Frenado Brusco"
        }, UsuarioActual);

        Assert.False(success);
        Assert.Contains("descartado", mensaje);
        _eventoRepo.Verify(r => r.Add(It.IsAny<Evento>()), Times.Never);
        _puntosRepo.Verify(r => r.AplicarImpactoEvento(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Registrar_ConFechaDeteccion_LaUsaComoFechaDelEvento()
    {
        ConCamara();
        ConViajeEnCurso(viajeId: 1, usuarioId: 7);
        Evento? capturado = null;
        _eventoRepo.Setup(r => r.Add(It.IsAny<Evento>()))
                   .Callback<Evento>(e => capturado = e)
                   .ReturnsAsync(1);

        // Hora de detección 10 min en el pasado (simula evento que estuvo en la cola).
        var fecha = DateTime.UtcNow.AddMinutes(-10);

        var (success, _, _, _) = await _sut.Registrar(new EventoRequestDto
        {
            CamaraId = 1, ViajeId = 1, TipoEvento = "Señal Respetada",
            FechaDeteccion = fecha.ToString("O")
        }, UsuarioActual);

        Assert.True(success);
        Assert.NotNull(capturado);
        // Se guarda la hora de detección del cliente, no la de llegada al servidor.
        Assert.Equal(fecha, capturado!.FechaEvento);
    }
}
