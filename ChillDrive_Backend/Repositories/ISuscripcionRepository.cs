using ChillDrive_Backend.Models;

namespace ChillDrive_Backend.Repositories;

public interface ISuscripcionRepository
{
    Task<List<Suscripcione>> GetAll();
    Task<List<Suscripcione>> GetByUsuarioId(int usuarioId);
    Task<Suscripcione?>      GetById(int id);
    Task<Suscripcione?>      GetActivaByUsuarioId(int usuarioId);
    Task<int>                Add(Suscripcione suscripcion);
    Task                     Update(Suscripcione suscripcion);
}