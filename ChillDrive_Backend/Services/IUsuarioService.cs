using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Services;

public interface IUsuarioService
{
    Task<List<UsuarioResponseDto>>                          GetAll();
    Task<UsuarioResponseDto?>                               GetById(int id);
    Task<(bool success, string mensaje, int? id)>           Registrar(UsuarioRequestDto dto);
    Task<(bool success, string mensaje)>                    Actualizar(int id, UsuarioRequestDto dto);
    Task<(bool success, string mensaje)>                    Eliminar(int id);
}