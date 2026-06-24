using ChillDrive_Backend.Models;

namespace ChillDrive_Backend.Repositories;

public interface IVehiculoRepository
{
    Task<List<Vehiculo>> GetAll();
    Task<List<Vehiculo>> GetByUsuarioId(int usuarioId);
    Task<Vehiculo?>      GetById(int id);
    Task<bool>           ExistePorPlaca(string placa);
    Task<int>            Add(Vehiculo vehiculo);
    Task                 Update(Vehiculo vehiculo);
    Task                 Delete(Vehiculo vehiculo);
}
