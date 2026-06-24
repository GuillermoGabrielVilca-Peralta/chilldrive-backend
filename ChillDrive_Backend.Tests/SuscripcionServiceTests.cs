using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Repositories;
using ChillDrive_Backend.Services.Implements;
using Moq;
using Xunit;

namespace ChillDrive_Backend.Tests;

// Prueba la suscripción: registrar (activa plan Profesional) y cancelar (revierte a Gratuito).
public class SuscripcionServiceTests
{
    private readonly Mock<ISuscripcionRepository> _repo        = new();
    private readonly Mock<IUsuarioRepository>     _usuarioRepo = new();
    private readonly SuscripcionService           _sut;

    public SuscripcionServiceTests()
    {
        _sut = new SuscripcionService(_repo.Object, _usuarioRepo.Object, new FakeUnitOfWork());
    }

    private static SuscripcionRequestDto DtoValido(int usuarioId = 1) => new()
    {
        UsuarioId   = usuarioId,
        FechaInicio = DateOnly.FromDateTime(DateTime.UtcNow),
        FechaFin    = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
        MontoSoles  = 15m,
        MetodoPago  = "Yape"
    };

    [Fact]
    public async Task Registrar_UsuarioInexistente_Falla()
    {
        _usuarioRepo.Setup(r => r.GetById(1)).ReturnsAsync((Usuario?)null);

        var (success, _, _) = await _sut.Registrar(DtoValido());

        Assert.False(success);
    }

    [Fact]
    public async Task Registrar_UsuarioYaTieneActiva_Falla()
    {
        _usuarioRepo.Setup(r => r.GetById(1)).ReturnsAsync(new Usuario { Id = 1, Correo = "a@a.com" });
        _repo.Setup(r => r.GetActivaByUsuarioId(1))
             .ReturnsAsync(new Suscripcione { Id = 9, UsuarioId = 1, Estado = "Activa" });

        var (success, _, _) = await _sut.Registrar(DtoValido());

        Assert.False(success);
    }

    [Fact]
    public async Task Registrar_Valido_ActivaPlanProfesional()
    {
        var usuario = new Usuario { Id = 1, Correo = "a@a.com", TipoPlan = "Gratuito" };
        _usuarioRepo.Setup(r => r.GetById(1)).ReturnsAsync(usuario);
        _repo.Setup(r => r.GetActivaByUsuarioId(1)).ReturnsAsync((Suscripcione?)null);

        var (success, _, _) = await _sut.Registrar(DtoValido());

        Assert.True(success);
        Assert.Equal("Profesional", usuario.TipoPlan);
        _repo.Verify(r => r.Add(It.IsAny<Suscripcione>()), Times.Once);
    }

    [Fact]
    public async Task Cancelar_SuscripcionNoActiva_Falla()
    {
        _repo.Setup(r => r.GetById(1)).ReturnsAsync(new Suscripcione { Id = 1, Estado = "Cancelada" });

        var (success, _) = await _sut.Cancelar(1);

        Assert.False(success);
    }

    [Fact]
    public async Task Cancelar_Valido_ReviertePlanAGratuito()
    {
        var usuario = new Usuario { Id = 1, Correo = "a@a.com", TipoPlan = "Profesional" };
        _repo.Setup(r => r.GetById(1))
             .ReturnsAsync(new Suscripcione { Id = 1, UsuarioId = 1, Estado = "Activa" });
        _usuarioRepo.Setup(r => r.GetById(1)).ReturnsAsync(usuario);

        var (success, _) = await _sut.Cancelar(1);

        Assert.True(success);
        Assert.Equal("Gratuito", usuario.TipoPlan);
    }
}
