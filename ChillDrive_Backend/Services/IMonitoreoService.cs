using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Services;

public interface IMonitoreoService
{
    Task<List<ViajeActivoResponseDto>> GetViajesActivos();
}
