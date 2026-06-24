using ChillDrive_Backend.Models;

namespace ChillDrive_Backend.Repositories;

public interface IUsuarioRepository
{
    Task<List<Usuario>> GetAll();
    Task<Usuario?>      GetById(int id);
    Task<Usuario?>      GetByCorreo(string correo);
    Task<bool>          ExistePorCorreo(string correo);
    Task<int>           Add(Usuario usuario);
    Task                Update(Usuario usuario);
    Task                Delete(Usuario usuario);
}
