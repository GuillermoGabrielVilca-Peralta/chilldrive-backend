using ChillDrive_Backend.Models;

namespace ChillDrive_Backend.Repositories;

public interface IAliadoRepository
{
    Task<List<Aliado>> GetAll();
    Task<List<Aliado>> GetByCategoria(string categoria);
    Task<Aliado?>      GetById(int id);
    Task<bool>         ExistePorRuc(string ruc);
    Task<int>          Add(Aliado aliado);
    Task               Update(Aliado aliado);
    Task               Delete(Aliado aliado);
}