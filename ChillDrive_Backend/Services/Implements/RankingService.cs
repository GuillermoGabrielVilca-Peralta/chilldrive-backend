using ChillDrive_Backend.Models.DTOs.Mappings;
using ChillDrive_Backend.Models.DTOs.Response;
using ChillDrive_Backend.Repositories;

namespace ChillDrive_Backend.Services.Implements;

public class RankingService : IRankingService
{
    private readonly IRankingRepository _repo;

    public RankingService(IRankingRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<RankingConductorResponseDto>> GetRanking()
    {
        var lista = await _repo.GetRanking();
        return lista.Select(v => v.ToResponse()).ToList();
    }
}
