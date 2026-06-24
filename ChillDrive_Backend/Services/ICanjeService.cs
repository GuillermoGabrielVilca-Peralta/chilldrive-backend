using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Services;

public interface ICanjeService
{
    Task<List<CanjeResponseDto>>                          GetAll();
    Task<List<CanjeResponseDto>>                          GetByUsuarioId(int usuarioId);
    Task<CanjeResponseDto?>                               GetById(int id);
    Task<(bool success, string mensaje, int? id)>         Canjear(CanjeRequestDto dto);
    Task<(bool success, string mensaje)>                  UsarQr(string codigoQr);
    Task<(bool success, string mensaje)>                  Anular(int id);
}