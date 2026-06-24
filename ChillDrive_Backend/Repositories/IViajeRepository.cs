using ChillDrive_Backend.Models;

namespace ChillDrive_Backend.Repositories;

public interface IViajeRepository
{
    Task<List<Viaje>> GetAll();
    Task<List<Viaje>>              GetByUsuarioId(int usuarioId);
    Task<(List<Viaje> items, int total)> GetByUsuarioIdPaginado(int usuarioId, int skip, int take);
    Task<List<Viaje>> GetByVehiculoId(int vehiculoId);
    Task<Viaje?>      GetById(int id);
    Task<int>         Add(Viaje viaje);
    Task              Update(Viaje viaje);
    Task              Delete(Viaje viaje);
}