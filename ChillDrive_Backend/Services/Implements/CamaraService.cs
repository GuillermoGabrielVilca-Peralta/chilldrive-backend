using ChillDrive_Backend.Models.DTOs.Mappings;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;
using ChillDrive_Backend.Repositories;

namespace ChillDrive_Backend.Services.Implements;

public class CamaraService : ICamaraService
{
    private readonly ICamaraRepository   _repo;
    private readonly IVehiculoRepository _vehiculoRepo;

    private static readonly string[] EstadosValidos = ["Conectada", "Desconectada", "Error"];

    public CamaraService(ICamaraRepository repo, IVehiculoRepository vehiculoRepo)
    {
        _repo         = repo;
        _vehiculoRepo = vehiculoRepo;
    }

    public async Task<List<CamaraResponseDto>> GetAll()
    {
        var lista = await _repo.GetAll();
        return lista.Select(c => c.ToResponse()).ToList();
    }

    public async Task<List<CamaraResponseDto>> GetByVehiculoId(int vehiculoId)
    {
        var lista = await _repo.GetByVehiculoId(vehiculoId);
        return lista.Select(c => c.ToResponse()).ToList();
    }

    public async Task<CamaraResponseDto?> GetById(int id)
    {
        var c = await _repo.GetById(id);
        return c?.ToResponse();
    }

    public async Task<(bool success, string mensaje, int? id)> Registrar(CamaraRequestDto dto)
    {
        var vehiculo = await _vehiculoRepo.GetById(dto.VehiculoId);
        if (vehiculo is null)
            return (false, "El vehículo no existe.", null);

        var id = await _repo.Add(dto.ToEntity());
        return (true, "Cámara registrada correctamente.", id);
    }

    public async Task<(bool success, string mensaje)> Actualizar(int id, CamaraRequestDto dto)
    {
        var camara = await _repo.GetById(id);
        if (camara is null)
            return (false, "Cámara no encontrada.");

        var vehiculo = await _vehiculoRepo.GetById(dto.VehiculoId);
        if (vehiculo is null)
            return (false, "El vehículo no existe.");

        dto.ApplyTo(camara);
        await _repo.Update(camara);
        return (true, "Cámara actualizada correctamente.");
    }

    public async Task<(bool success, string mensaje)> Eliminar(int id)
    {
        var camara = await _repo.GetById(id);
        if (camara is null)
            return (false, "Cámara no encontrada.");

        await _repo.Delete(camara);
        return (true, "Cámara eliminada correctamente.");
    }

    // -------------------------------------------------------------------------
    // Endpoint específico: cambiar estado de conexión (Conectada/Desconectada/Error)
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje)> CambiarEstado(int id, string nuevoEstado)
    {
        if (!EstadosValidos.Contains(nuevoEstado))
            return (false, $"Estado inválido. Valores permitidos: {string.Join(", ", EstadosValidos)}");

        var camara = await _repo.GetById(id);
        if (camara is null)
            return (false, "Cámara no encontrada.");

        camara.Estado = nuevoEstado;
        await _repo.Update(camara);
        return (true, $"Estado actualizado a '{nuevoEstado}' correctamente.");
    }
}