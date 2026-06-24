using ChillDrive_Backend.Models.DTOs.Mappings;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;
using ChillDrive_Backend.Repositories;

namespace ChillDrive_Backend.Services.Implements;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _repo;
    private readonly IPasswordService   _passwordService;

    /// <summary>
    /// Se inyectan el repositorio de usuarios y el servicio de hashing.
    /// IPasswordService abstrae BCrypt — si se cambia el algoritmo, solo se toca PasswordService.
    /// </summary>
    public UsuarioService(IUsuarioRepository repo, IPasswordService passwordService)
    {
        _repo            = repo;
        _passwordService = passwordService;
    }

    // -------------------------------------------------------------------------
    // GET ALL — devuelve todos los usuarios sin exponer contraseñas
    // -------------------------------------------------------------------------
    public async Task<List<UsuarioResponseDto>> GetAll()
    {
        var usuarios = await _repo.GetAll();
        return usuarios.Select(u => u.ToResponse()).ToList();
    }

    // -------------------------------------------------------------------------
    // GET BY ID — devuelve un usuario por su clave primaria
    // -------------------------------------------------------------------------
    public async Task<UsuarioResponseDto?> GetById(int id)
    {
        var usuario = await _repo.GetById(id);
        return usuario?.ToResponse();
    }

    // -------------------------------------------------------------------------
    // REGISTRAR
    // Valida correo único, hashea la contraseña y persiste el nuevo usuario.
    // El hash se genera aquí (capa de servicio) antes de llegar al mapper o al repo.
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje, int? id)> Registrar(UsuarioRequestDto dto)
    {
        // Verifica que el correo no esté en uso por otro usuario
        if (await _repo.ExistePorCorreo(dto.Correo))
            return (false, "El correo ya está registrado.", null);

        // Hashea la contraseña en texto plano antes de guardar en BD
        var passwordHash = _passwordService.Hash(dto.Password);

        var id = await _repo.Add(dto.ToEntity(passwordHash));
        return (true, "Usuario registrado correctamente.", id);
    }

    // -------------------------------------------------------------------------
    // ACTUALIZAR
    // Si el correo cambia, verifica que no lo use otro usuario.
    // Siempre rehashea la contraseña para mantener el hash actualizado.
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje)> Actualizar(int id, UsuarioRequestDto dto)
    {
        var usuario = await _repo.GetById(id);
        if (usuario is null)
            return (false, "Usuario no encontrado.");

        // Si cambia el correo, verifica que no esté en uso por otro usuario
        if (usuario.Correo != dto.Correo && await _repo.ExistePorCorreo(dto.Correo))
            return (false, "El correo ya está en uso por otro usuario.");

        // Rehashea la contraseña antes de actualizar
        var passwordHash = _passwordService.Hash(dto.Password);

        dto.ApplyTo(usuario, passwordHash);
        await _repo.Update(usuario);
        return (true, "Usuario actualizado correctamente.");
    }

    // -------------------------------------------------------------------------
    // ELIMINAR — elimina el registro del usuario de la BD
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje)> Eliminar(int id)
    {
        var usuario = await _repo.GetById(id);
        if (usuario is null)
            return (false, "Usuario no encontrado.");

        await _repo.Delete(usuario);
        return (true, "Usuario eliminado correctamente.");
    }
}
