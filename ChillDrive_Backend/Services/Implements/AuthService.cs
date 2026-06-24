using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Mappings;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;
using ChillDrive_Backend.Repositories;
using Microsoft.IdentityModel.Tokens;

namespace ChillDrive_Backend.Services.Implements;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IPasswordService   _passwordService;
    private readonly IConfiguration     _config;

    /// <summary>
    /// Se inyecta IConfiguration para leer la sección "Jwt" de appsettings.json
    /// sin acoplar el servicio a ninguna clase concreta de configuración.
    /// </summary>
    public AuthService(
        IUsuarioRepository usuarioRepo,
        IPasswordService   passwordService,
        IConfiguration     config)
    {
        _usuarioRepo     = usuarioRepo;
        _passwordService = passwordService;
        _config          = config;
    }

    // -------------------------------------------------------------------------
    // LOGIN
    // Valida correo + contraseña y devuelve un token JWT si son correctos.
    // El mensaje de error es intencionalmente genérico ("correo o contraseña
    // incorrectos") para no revelar si el correo existe o no en el sistema
    // (prevención de enumeración de usuarios).
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje, LoginResponseDto? data)> Login(LoginRequestDto dto)
    {
        // Busca el usuario por correo
        var usuario = await _usuarioRepo.GetByCorreo(dto.Correo);

        // Si no existe el correo o la contraseña no coincide, mismo mensaje de error
        if (usuario is null || !_passwordService.Verificar(dto.Password, usuario.PasswordHash!))
            return (false, "Correo o contraseña incorrectos.", null);

        // Verifica que la cuenta esté activa antes de emitir el token
        if (usuario.Estado != "Activo")
            return (false, "La cuenta está inactiva. Contacta al administrador.", null);

        // Genera el token JWT con los datos del usuario como claims
        var (token, expiracion) = GenerarToken(usuario);

        // Actualiza el campo UltimoAcceso para auditoría
        usuario.UltimoAcceso = DateTime.UtcNow;
        await _usuarioRepo.Update(usuario);

        // El armado del DTO de respuesta vive en el mapper (no en el servicio).
        return (true, "Login exitoso.", usuario.ToLoginResponse(token, expiracion));
    }

    // -------------------------------------------------------------------------
    // GENERACIÓN DEL TOKEN JWT
    // Los claims son datos que viajan dentro del token y el servidor puede leer
    // sin ir a la BD. Incluimos lo mínimo necesario para la autorización.
    // -------------------------------------------------------------------------
    private (string token, DateTime expiracion) GenerarToken(Usuario usuario)
    {
        var jwtSettings     = _config.GetSection("Jwt");
        var secretKey       = jwtSettings["SecretKey"]!;
        var issuer          = jwtSettings["Issuer"]!;
        var audience        = jwtSettings["Audience"]!;
        var expirationHours = int.Parse(jwtSettings["ExpirationHours"]!);

        // Clave simétrica usada para firmar el token (HMAC-SHA256)
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Determina el rol: si el correo está en la lista "Admin:Correos" de appsettings,
        // el usuario es Admin; en caso contrario, Usuario normal.
        var adminCorreos = _config.GetSection("Admin:Correos").Get<string[]>() ?? [];
        var rol = adminCorreos.Contains(usuario.Correo, StringComparer.OrdinalIgnoreCase)
            ? Roles.Admin
            : Roles.Usuario;

        // Claims que se incrustan en el payload del JWT
        var claims = new[]
        {
            // Identificador único del usuario — se usa con User.FindFirst(ClaimTypes.NameIdentifier)
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email,          usuario.Correo),

            // Rol para autorización — habilita [Authorize(Roles = Roles.Admin)] en los endpoints
            new Claim(ClaimTypes.Role, rol),

            // Claims personalizados para que el frontend pueda adaptar la UI sin hacer otra llamada
            new Claim("tipoPlan",      usuario.TipoPlan      ?? "Gratuito"),
            new Claim("tipoConductor", usuario.TipoConductor ?? "Particular"),

            // JTI (JWT ID) — identificador único por token, útil para invalidación futura
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expiracion = DateTime.UtcNow.AddHours(expirationHours);

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            expires:            expiracion,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiracion);
    }
}
