using ChillDrive_Backend.Models;

namespace ChillDrive_Backend.Repositories;

public interface IEventoRepository
{
    Task<List<Evento>> GetAll();
    Task<List<Evento>>              GetByViajeId(int viajeId);
    Task<(List<Evento> items, int total)> GetByViajeIdPaginado(int viajeId, int skip, int take);
    Task<List<Evento>> GetByCamaraId(int camaraId);
    Task<Evento?>      GetById(int id);
    Task<Evento?>      GetByClienteEventoId(string clienteEventoId);
    Task<bool>         ExisteEventoTipoEnVentana(int viajeId, string tipoEvento, DateTime desde, DateTime hasta);
    Task<int>          Add(Evento evento);
    Task               Delete(Evento evento);
    Task<List<(string Tipo, int TotalImpacto, int Cantidad)>> GetImpactoNegativoPorTipoParaUsuario(int usuarioId);
}
