using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Models.DTOs.Mappings;

public static class UsuarioMappings
{
    /// <summary>
    /// Convierte la entidad Usuario al DTO de respuesta que se envía al cliente.
    /// Nunca incluye el PasswordHash — ese campo no sale de la API.
    /// </summary>
    public static UsuarioResponseDto ToResponse(this Usuario u) => new()
    {
        Id            = u.Id,
        Nombres       = u.Nombres,
        Apellidos     = u.Apellidos,
        Correo        = u.Correo,
        Telefono      = u.Telefono,
        FotoPerfil    = u.FotoPerfil,
        TipoPlan      = u.TipoPlan,
        TipoConductor = u.TipoConductor,
        Estado        = u.Estado,
        FechaRegistro = u.FechaRegistro,
        UltimoAcceso  = u.UltimoAcceso
    };

    /// <summary>
    /// Arma la respuesta de login: combina el token y su expiración (generados por AuthService)
    /// con los datos básicos del usuario. El servicio pasa el token ya creado; el mapper solo
    /// estructura el DTO, no genera nada ni aplica reglas de negocio.
    /// </summary>
    public static LoginResponseDto ToLoginResponse(this Usuario u, string token, DateTime expiracion) => new()
    {
        Token          = token,
        Expiracion     = expiracion,
        UsuarioId      = u.Id,
        NombreCompleto = $"{u.Nombres} {u.Apellidos}",
        Correo         = u.Correo,
        TipoPlan       = u.TipoPlan      ?? "Gratuito",
        TipoConductor  = u.TipoConductor ?? "Particular"
    };

    /// <summary>
    /// Crea una entidad Usuario nueva a partir del DTO de solicitud.
    /// Recibe el hash ya calculado por el servicio — el mapper no hashea.
    /// </summary>
    public static Usuario ToEntity(this UsuarioRequestDto dto, string passwordHash) => new()
    {
        Nombres       = dto.Nombres,
        Apellidos     = dto.Apellidos,
        Correo        = dto.Correo,
        PasswordHash  = passwordHash,   // hash BCrypt generado por PasswordService
        Telefono      = dto.Telefono,
        FotoPerfil    = dto.FotoPerfil,
        TipoPlan      = dto.TipoPlan,
        TipoConductor = dto.TipoConductor,
        Estado        = dto.Estado
    };

    /// <summary>
    /// Aplica los cambios del DTO sobre una entidad existente (para PUT/actualización).
    /// Recibe el hash ya calculado — permite actualizar la contraseña de forma segura.
    /// </summary>
    public static void ApplyTo(this UsuarioRequestDto dto, Usuario usuario, string passwordHash)
    {
        usuario.Nombres       = dto.Nombres;
        usuario.Apellidos     = dto.Apellidos;
        usuario.Correo        = dto.Correo;
        usuario.PasswordHash  = passwordHash;   // hash BCrypt recalculado por el servicio
        usuario.Telefono      = dto.Telefono;
        usuario.FotoPerfil    = dto.FotoPerfil;
        usuario.TipoPlan      = dto.TipoPlan;
        usuario.TipoConductor = dto.TipoConductor;
        usuario.Estado        = dto.Estado;
    }
}
