using ChillDrive_Backend.Models;

namespace ChillDrive_Backend.Repositories;

public interface IPremioRepository
{
    Task<List<Premio>> GetAll();
    Task<List<Premio>> GetByAliadoId(int aliadoId);
    Task<List<Premio>>              GetDisponibles();
    Task<(List<Premio> items, int total)> GetDisponiblesPaginado(int skip, int take);
    Task<Premio?>      GetById(int id);
    Task<int>          Add(Premio premio);
    Task               Update(Premio premio);
    Task               Delete(Premio premio);
}