using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Services;

/// <summary>
/// Contrato del servicio de autenticación.
/// Separado de IUsuarioService porque autenticación es una responsabilidad distinta
/// a la gestión de datos del usuario (principio de responsabilidad única).
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Valida las credenciales y, si son correctas, genera un token JWT.
    /// Devuelve el token junto con los datos básicos del usuario para la sesión.
    /// </summary>
    Task<(bool success, string mensaje, LoginResponseDto? data)> Login(LoginRequestDto dto);

    /// <summary>
    /// Valida un refresh token vigente y, si es correcto, emite un nuevo par de tokens
    /// (access + refresh rotado). Evita que el usuario tenga que re-loguear cada día.
    /// </summary>
    Task<(bool success, string mensaje, LoginResponseDto? data)> Refrescar(string refreshToken);
}
