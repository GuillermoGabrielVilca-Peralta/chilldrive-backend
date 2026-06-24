using ChillDrive_Backend.Models;

namespace ChillDrive_Backend.Repositories;

public interface ICamaraRepository
{
    Task<List<Camara>> GetAll();
    Task<List<Camara>> GetByVehiculoId(int vehiculoId);
    Task<Camara?>      GetById(int id);
    Task<int>          Add(Camara camara);
    Task               Update(Camara camara);
    Task               Delete(Camara camara);
}