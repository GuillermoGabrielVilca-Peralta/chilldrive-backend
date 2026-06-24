using ChillDrive_Backend.Models;

namespace ChillDrive_Backend.Repositories;

public interface ICanjeRepository
{
    Task<List<Canje>> GetAll();
    Task<List<Canje>> GetByUsuarioId(int usuarioId);
    Task<Canje?>      GetById(int id);
    Task<Canje?>      GetByCodigoQr(string codigoQr);
    Task<int>         Add(Canje canje);
    Task              Update(Canje canje);
}
