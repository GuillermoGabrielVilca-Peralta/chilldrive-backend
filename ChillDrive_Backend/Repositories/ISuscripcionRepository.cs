using ChillDrive_Backend.Models;

namespace ChillDrive_Backend.Repositories;

public interface ISuscripcionRepository
{
    Task<List<Suscripcione>> GetAll();
    Task<List<Suscripcione>> GetByUsuarioId(int usuarioId);
    Task<Suscripcione?>      GetById(int id);
    Task<Suscripcione?>      GetActivaByUsuarioId(int usuarioId);
    Task<Suscripcione?>      GetPendienteByUsuarioId(int usuarioId);
    Task<List<Suscripcione>> GetPendientes();
    Task<bool>               UsoPrueba(int usuarioId);
    Task<bool>               ExisteReferenciaYape(string referencia);
    Task<bool>               ExisteCodigoPago(string codigo);

    // Activas cuyo periodo [inicio, fin) ya terminó al día `hoy` (fin <= hoy).
    Task<List<Suscripcione>> GetActivasVencidas(DateOnly hoy);

    // ¿Le queda alguna suscripción activa vigente (fin > hoy)?
    Task<bool>               TieneVigente(int usuarioId, DateOnly hoy);

    Task<int>                Add(Suscripcione suscripcion);
    Task                     Update(Suscripcione suscripcion);
}
