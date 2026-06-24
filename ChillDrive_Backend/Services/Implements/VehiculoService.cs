using ChillDrive_Backend.Models.DTOs.Mappings;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;
using ChillDrive_Backend.Repositories;

namespace ChillDrive_Backend.Services.Implements;

public class VehiculoService : IVehiculoService
{
    private readonly IVehiculoRepository _repo;
    private readonly IUsuarioRepository  _usuarioRepo;

    public VehiculoService(IVehiculoRepository repo, IUsuarioRepository usuarioRepo)
    {
        _repo        = repo;
        _usuarioRepo = usuarioRepo;
    }

    public async Task<List<VehiculoResponseDto>> GetAll()
    {
        var lista = await _repo.GetAll();
        return lista.Select(v => v.ToResponse()).ToList();
    }

    public async Task<List<VehiculoResponseDto>> GetByUsuarioId(int usuarioId)
    {
        var lista = await _repo.GetByUsuarioId(usuarioId);
        return lista.Select(v => v.ToResponse()).ToList();
    }

    public async Task<VehiculoResponseDto?> GetById(int id)
    {
        var v = await _repo.GetById(id);
        return v?.ToResponse();
    }

    public async Task<(bool success, string mensaje, int? id)> Registrar(VehiculoRequestDto dto)
    {
        // Verificar que el usuario existe
        var usuario = await _usuarioRepo.GetById(dto.UsuarioId);
        if (usuario is null)
            return (false, "El usuario no existe.", null);

        // Verificar placa única
        if (await _repo.ExistePorPlaca(dto.Placa))
            return (false, "La placa ya está registrada.", null);

        var id = await _repo.Add(dto.ToEntity());
        return (true, "Vehículo registrado correctamente.", id);
    }

    public async Task<(bool success, string mensaje)> Actualizar(int id, VehiculoRequestDto dto)
    {
        var vehiculo = await _repo.GetById(id);
        if (vehiculo is null)
            return (false, "Vehículo no encontrado.");

        // Si cambia la placa, verificar que no la use otro vehículo
        if (vehiculo.Placa != dto.Placa && await _repo.ExistePorPlaca(dto.Placa))
            return (false, "La placa ya está en uso por otro vehículo.");

        dto.ApplyTo(vehiculo);
        await _repo.Update(vehiculo);
        return (true, "Vehículo actualizado correctamente.");
    }

    public async Task<(bool success, string mensaje)> Eliminar(int id)
    {
        var vehiculo = await _repo.GetById(id);
        if (vehiculo is null)
            return (false, "Vehículo no encontrado.");

        await _repo.Delete(vehiculo);
        return (true, "Vehículo eliminado correctamente.");
    }
}