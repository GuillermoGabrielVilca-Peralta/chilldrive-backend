using ChillDrive_Backend.Models;

namespace ChillDrive_Backend.Repositories;

/// <summary>
/// Acceso de solo lectura a la vista v_ranking_conductores (entidad keyless).
/// </summary>
public interface IRankingRepository
{
    Task<List<VRankingConductore>> GetRanking();
}
