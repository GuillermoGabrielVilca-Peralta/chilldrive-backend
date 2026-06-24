using ChillDrive_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace ChillDrive_Backend.Repositories.Implements;

public class RankingRepository : IRankingRepository
{
    private readonly ApplicationDbContext _context;

    public RankingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Lee la vista ordenada por puntos históricos (el mejor conductor primero).
    public async Task<List<VRankingConductore>> GetRanking()
    {
        return await _context.VRankingConductores
            .OrderByDescending(v => v.PuntosHistoricos)
            .ToListAsync();
    }
}
