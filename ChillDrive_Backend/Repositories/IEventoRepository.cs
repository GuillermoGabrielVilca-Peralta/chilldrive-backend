using ChillDrive_Backend.Models;

namespace ChillDrive_Backend.Repositories;

public interface IEventoRepository
{
    Task<List<Evento>> GetAll();
    Task<List<Evento>>              GetByViajeId(int viajeId);
    Task<(List<Evento> items, int total)> GetByViajeIdPaginado(int viajeId, int skip, int take);
    Task<List<Evento>> GetByCamaraId(int camaraId);
    Task<Evento?>      GetById(int id);
    Task<int>          Add(Evento evento);
    Task               Delete(Evento evento);
}
