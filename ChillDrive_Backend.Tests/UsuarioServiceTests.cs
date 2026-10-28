using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Repositories;
using ChillDrive_Backend.Services;          // IPasswordService
using ChillDrive_Backend.Services.Implements;
using Moq;
using Xunit;

namespace ChillDrive_Backend.Tests;

// Cubre las validaciones de UsuarioService: correo único (registro y actualización,
// incluyendo el atajo de "mismo correo"), hasheo de contraseña y existencia al eliminar.
public class UsuarioServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepo     = new();
    private readonly Mock<IPasswordService>   _passwordService = new();
    private readonly UsuarioService           _sut;

    public UsuarioServiceTests()
    {
        _passwordService.Setup(p => p.Hash(It.IsAny<string>())).Returns("hashed");
        _sut = new UsuarioService(_usuarioRepo.Object, _passwordService.Object);
    }

    private static UsuarioRequestDto DtoValido(string correo = "ana@correo.com") =>
        new()
        {
            Nombres   = "Ana",
            Apellidos = "Pérez",
            Correo    = correo,
            Password  = "secreta123",
        };

    // ── Registrar ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Registrar_CorreoDuplicado_Falla()
    {
        _usuarioRepo.Setup(r => r.ExistePorCorreo("ana@correo.com")).ReturnsAsync(true);

        var (success, _, _) = await _sut.Registrar(DtoValido());

        Assert.False(success);
        _usuarioRepo.Verify(r => r.Add(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task Registrar_Valido_HasheaLaPasswordYCrea()
    {
        _usuarioRepo.Setup(r => r.ExistePorCorreo("ana@correo.com")).ReturnsAsync(false);
        _usuarioRepo.Setup(r => r.Add(It.IsAny<Usuario>())).ReturnsAsync(3);

        var (success, _, id) = await _sut.Registrar(DtoValido());

        Assert.True(success);
        Assert.Equal(3, id);
        _passwordService.Verify(p => p.Hash("secreta123"), Times.Once);
        _usuarioRepo.Verify(r => r.Add(It.IsAny<Usuario>()), Times.Once);
    }

    // ── Actualizar ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Actualizar_NoEncontrado_Falla()
    {
        _usuarioRepo.Setup(r => r.GetById(5)).ReturnsAsync((Usuario?)null);

        var (success, _) = await _sut.Actualizar(5, DtoValido());

        Assert.False(success);
    }

    [Fact]
    public async Task Actualizar_CorreoEnUsoPorOtro_Falla()
    {
        _usuarioRepo.Setup(r => r.GetById(5))
                    .ReturnsAsync(new Usuario { Id = 5, Correo = "vieja@correo.com" });
        _usuarioRepo.Setup(r => r.ExistePorCorreo("nueva@correo.com")).ReturnsAsync(true);

        var (success, _) = await _sut.Actualizar(5, DtoValido("nueva@correo.com"));

        Assert.False(success);
        _usuarioRepo.Verify(r => r.Update(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task Actualizar_MismoCorreo_NoChequeaDuplicadoYActualiza()
    {
        _usuarioRepo.Setup(r => r.GetById(5))
                    .ReturnsAsync(new Usuario { Id = 5, Correo = "ana@correo.com" });

        var (success, _) = await _sut.Actualizar(5, DtoValido("ana@correo.com"));

        Assert.True(success);
        _usuarioRepo.Verify(r => r.ExistePorCorreo(It.IsAny<string>()), Times.Never);
        _usuarioRepo.Verify(r => r.Update(It.IsAny<Usuario>()), Times.Once);
    }

    // ── Seguridad: plan y estado no los decide el cliente ─────────────────────

    [Fact]
    public async Task Registrar_CuentaNueva_SiempreGratuitaYActiva()
    {
        await _sut.Registrar(DtoValido());

        _usuarioRepo.Verify(r => r.Add(It.Is<Usuario>(u =>
            u.TipoPlan == "Gratuito" && u.Estado == "Activo")), Times.Once);
    }

    [Fact]
    public async Task Actualizar_NoCambiaPlanPagadoNiReactivaSuspendido()
    {
        var usuario = new Usuario { Id = 5, Correo = "ana@correo.com", TipoPlan = "Profesional", Estado = "Suspendido" };
        _usuarioRepo.Setup(r => r.GetById(5)).ReturnsAsync(usuario);

        var (success, _) = await _sut.Actualizar(5, DtoValido("ana@correo.com"));

        Assert.True(success);
        Assert.Equal("Profesional", usuario.TipoPlan);
        Assert.Equal("Suspendido", usuario.Estado);
    }

    // ── Eliminar ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Eliminar_NoEncontrado_Falla()
    {
        _usuarioRepo.Setup(r => r.GetById(5)).ReturnsAsync((Usuario?)null);

        var (success, _) = await _sut.Eliminar(5);

        Assert.False(success);
    }

    [Fact]
    public async Task Eliminar_Valido_EliminaUsuario()
    {
        _usuarioRepo.Setup(r => r.GetById(5))
                    .ReturnsAsync(new Usuario { Id = 5, Correo = "ana@correo.com" });

        var (success, _) = await _sut.Eliminar(5);

        Assert.True(success);
        _usuarioRepo.Verify(r => r.Delete(It.IsAny<Usuario>()), Times.Once);
    }
}
