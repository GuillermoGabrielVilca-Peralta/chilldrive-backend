namespace ChillDrive_Backend.Models.DTOs.Response;

/// <summary>
/// Respuesta que recibe el cliente tras un login exitoso.
/// Incluye el token JWT y los datos básicos del usuario para que
/// la app pueda mostrar la sesión sin hacer otra llamada extra.
/// </summary>
public class LoginResponseDto
{
    /// <summary>Token JWT que el cliente debe enviar en el header Authorization: Bearer {token}</summary>
    public string   Token          { get; set; } = null!;

    /// <summary>Fecha y hora UTC en que el token expira — el cliente puede usarla para renovar antes.</summary>
    public DateTime Expiracion     { get; set; }

    /// <summary>Token de larga vida para renovar el acceso sin re-loguear (POST /api/auth/refresh).</summary>
    public string   RefreshToken   { get; set; } = null!;

    /// <summary>Fecha y hora UTC en que el refresh token expira (tras esto, re-login obligatorio).</summary>
    public DateTime RefreshExpiracion { get; set; }

    // Datos de la sesión activa
    public int      UsuarioId      { get; set; }
    public string   NombreCompleto { get; set; } = null!;
    public string   Correo         { get; set; } = null!;
    public string   TipoPlan       { get; set; } = null!;
    public string   TipoConductor  { get; set; } = null!;
}
