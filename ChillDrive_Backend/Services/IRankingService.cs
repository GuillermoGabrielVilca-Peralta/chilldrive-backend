using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Services;

public interface IRankingService
{
    Task<List<RankingConductorResponseDto>> GetRanking();
}
