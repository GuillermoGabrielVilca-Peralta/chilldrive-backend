using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Repositories;
using ChillDrive_Backend.Services.Implements;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ChillDrive_Backend.Tests;

// Prueba el login (par access+refresh) y el flujo de renovación de tokens.
public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepo = new();
    private readonly Mock<IPasswordService>   _passwordService = new();
    private readonly AuthService              _sut;

    public AuthServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"]             = "clave-de-prueba-suficientemente-larga-para-hmac-256-0123456789",
                ["Jwt:Issuer"]               = "ChillDriveAPI",
                ["Jwt:Audience"]             = "ChillDriveCliente",
                ["Jwt:ExpirationHours"]      = "24",
                ["Jwt:RefreshAudience"]      = "ChillDriveRefresh",
                ["Jwt:RefreshExpirationDays"] = "30",
                ["Admin:Correos:0"]          = "admin@chilldrive.pe",
            })
            .Build();

        _sut = new AuthService(_usuarioRepo.Object, _passwordService.Object, config);
    }

    private static Usuario UsuarioActivo(int id = 1) => new()
    {
        Id            = id,
        Nombres       = "Ana",
        Apellidos     = "Pérez",
        Correo        = "ana@correo.com",
        PasswordHash  = "hash",
        TipoPlan      = "Gratuito",
        TipoConductor = "Particular",
        Estado        = "Activo",
    };

    private async Task<LoginResponseDtoLike> LoginOk()
    {
        _usuarioRepo.Setup(r => r.GetByCorreo("ana@correo.com")).ReturnsAsync(UsuarioActivo());
        _passwordService.Setup(p => p.Verificar(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        var (success, _, data) = await _sut.Login(
            new LoginRequestDto { Correo = "ana@correo.com", Password = "x" });
        Assert.True(success);
        return new LoginResponseDtoLike(data!.Token, data.RefreshToken);
    }

    private readonly record struct LoginResponseDtoLike(string Token, string RefreshToken);

    [Fact]
    public async Task Login_CredencialesValidas_DevuelveAccessYRefresh()
    {
        var tokens = await LoginOk();
        Assert.False(string.IsNullOrWhiteSpace(tokens.Token));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        Assert.NotEqual(tokens.Token, tokens.RefreshToken);
    }

    [Fact]
    public async Task Refrescar_ConRefreshTokenValido_DevuelveNuevoPar()
    {
        var tokens = await LoginOk();
        _usuarioRepo.Setup(r => r.GetById(1)).ReturnsAsync(UsuarioActivo());

        var (success, _, data) = await _sut.Refrescar(tokens.RefreshToken);

        Assert.True(success);
        Assert.False(string.IsNullOrWhiteSpace(data!.Token));
        Assert.False(string.IsNullOrWhiteSpace(data.RefreshToken));
    }

    [Fact]
    public async Task Refrescar_UsandoAccessTokenComoRefresh_Falla()
    {
        // El access token tiene otra audience → no debe aceptarse en /refresh.
        var tokens = await LoginOk();

        var (success, mensaje, _) = await _sut.Refrescar(tokens.Token);

        Assert.False(success);
        Assert.Contains("inválido", mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refrescar_ConTokenBasura_Falla()
    {
        var (success, _, _) = await _sut.Refrescar("esto-no-es-un-jwt");
        Assert.False(success);
    }

    [Fact]
    public async Task Refrescar_Vacio_Falla()
    {
        var (success, _, _) = await _sut.Refrescar("");
        Assert.False(success);
    }

    [Fact]
    public async Task Refrescar_UsuarioInactivo_Falla()
    {
        var tokens = await LoginOk();
        var inactivo = UsuarioActivo();
        inactivo.Estado = "Inactivo";
        _usuarioRepo.Setup(r => r.GetById(1)).ReturnsAsync(inactivo);

        var (success, mensaje, _) = await _sut.Refrescar(tokens.RefreshToken);

        Assert.False(success);
        Assert.Contains("inactiva", mensaje, StringComparison.OrdinalIgnoreCase);
    }
}
