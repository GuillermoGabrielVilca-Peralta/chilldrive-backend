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

    // Datos de la sesión activa
    public int      UsuarioId      { get; set; }
    public string   NombreCompleto { get; set; } = null!;
    public string   Correo         { get; set; } = null!;
    public string   TipoPlan       { get; set; } = null!;
    public string   TipoConductor  { get; set; } = null!;
}
