using ChillDrive_Backend.Models;

namespace ChillDrive_Backend.Repositories;

/// <summary>
/// Acceso de solo lectura a la vista v_suscripciones_activas (entidad keyless).
/// </summary>
public interface ISuscripcionActivaRepository
{
    Task<List<VSuscripcionesActiva>> GetActivas();
}
