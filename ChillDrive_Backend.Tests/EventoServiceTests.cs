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

    private void ConViajeEnCurso(int viajeId = 1, int usuarioId = 7) =>
        _viajeRepo.Setup(r => r.GetById(viajeId))
                  .ReturnsAsync(new Viaje { Id = viajeId, UsuarioId = usuarioId, Estado = "En Curso" });

    [Fact]
    public async Task Registrar_SenalRespetada_SumaPuntosYRegistraGanado()
    {
        ConCamara();
        ConViajeEnCurso(viajeId: 1, usuarioId: 7);

        var (success, _, _) = await _sut.Registrar(new EventoRequestDto
        {
            CamaraId = 1, ViajeId = 1, TipoEvento = "Señal Respetada"
        });

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

        var (success, _, _) = await _sut.Registrar(new EventoRequestDto
        {
            CamaraId = 1, ViajeId = 1, TipoEvento = "Señal Ignorada"
        });

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

        var (success, _, _) = await _sut.Registrar(new EventoRequestDto
        {
            CamaraId = 1, ViajeId = null, TipoEvento = "Señal Respetada"
        });

        Assert.True(success);
        _puntosRepo.Verify(r => r.AplicarImpactoEvento(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Registrar_TipoDeEventoInvalido_Falla()
    {
        var (success, _, _) = await _sut.Registrar(new EventoRequestDto
        {
            CamaraId = 1, TipoEvento = "NoExiste"
        });

        Assert.False(success);
    }
}
