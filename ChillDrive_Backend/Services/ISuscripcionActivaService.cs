using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Services;

public interface ISuscripcionActivaService
{
    Task<List<SuscripcionActivaResponseDto>> GetActivas();
}
