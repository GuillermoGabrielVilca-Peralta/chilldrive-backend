using ChillDrive_Backend.Models;

namespace ChillDrive_Backend.Repositories;

public interface IMovimientoPuntoRepository
{
    Task<List<MovimientosPunto>> GetByUsuarioId(int usuarioId);
    Task<List<MovimientosPunto>> GetByUsuarioIdYTipo(int usuarioId, string tipo);
    Task<MovimientosPunto?>      GetById(int id);
    Task<int>                    Add(MovimientosPunto movimiento);
}
