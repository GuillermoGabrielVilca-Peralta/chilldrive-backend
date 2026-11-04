using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Services;

public interface IRendimientoService
{
    Task<(bool success, string mensaje, RendimientoResponseDto? data)> Get(int dias, double kmPorGalon, double precioGalon);
}
