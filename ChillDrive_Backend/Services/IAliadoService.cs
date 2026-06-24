using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Services;

public interface IAliadoService
{
    Task<List<AliadoResponseDto>>                        GetAll();
    Task<List<AliadoResponseDto>>                        GetByCategoria(string categoria);
    Task<AliadoResponseDto?>                             GetById(int id);
    Task<(bool success, string mensaje, int? id)>        Registrar(AliadoRequestDto dto);
    Task<(bool success, string mensaje)>                 Actualizar(int id, AliadoRequestDto dto);
    Task<(bool success, string mensaje)>                 Eliminar(int id);
    Task<(bool success, string mensaje)>                 CambiarEstado(int id, string nuevoEstado);
}