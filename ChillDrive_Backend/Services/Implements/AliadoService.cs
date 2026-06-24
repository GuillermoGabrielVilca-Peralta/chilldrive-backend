using ChillDrive_Backend.Models.DTOs.Mappings;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;
using ChillDrive_Backend.Repositories;

namespace ChillDrive_Backend.Services.Implements;

public class AliadoService : IAliadoService
{
    private readonly IAliadoRepository _repo;

    private static readonly string[] EstadosValidos    = ["Activo", "Inactivo", "Suspendido"];
    private static readonly string[] CategoriasValidas =
    [
        "Gastronomía", "Combustible", "Entretenimiento",
        "Salud", "Transporte", "Retail", "Otro"
    ];

    public AliadoService(IAliadoRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<AliadoResponseDto>> GetAll()
    {
        var lista = await _repo.GetAll();
        return lista.Select(a => a.ToResponse()).ToList();
    }

    public async Task<List<AliadoResponseDto>> GetByCategoria(string categoria)
    {
        var lista = await _repo.GetByCategoria(categoria);
        return lista.Select(a => a.ToResponse()).ToList();
    }

    public async Task<AliadoResponseDto?> GetById(int id)
    {
        var a = await _repo.GetById(id);
        return a?.ToResponse();
    }

    public async Task<(bool success, string mensaje, int? id)> Registrar(AliadoRequestDto dto)
    {
        if (!CategoriasValidas.Contains(dto.Categoria))
            return (false, $"Categoría inválida. Valores permitidos: {string.Join(", ", CategoriasValidas)}", null);

        // El rango 0-100 de la comisión ya lo valida [Range] en AliadoRequestDto.

        // RUC único si se proporciona
        if (!string.IsNullOrWhiteSpace(dto.Ruc) && await _repo.ExistePorRuc(dto.Ruc))
            return (false, "El RUC ya está registrado.", null);

        var id = await _repo.Add(dto.ToEntity());
        return (true, "Aliado registrado correctamente.", id);
    }

    public async Task<(bool success, string mensaje)> Actualizar(int id, AliadoRequestDto dto)
    {
        var aliado = await _repo.GetById(id);
        if (aliado is null)
            return (false, "Aliado no encontrado.");

        if (!CategoriasValidas.Contains(dto.Categoria))
            return (false, $"Categoría inválida. Valores permitidos: {string.Join(", ", CategoriasValidas)}");

        // El rango 0-100 de la comisión ya lo valida [Range] en AliadoRequestDto.

        // Si cambia el RUC verificar que no lo use otro aliado
        if (!string.IsNullOrWhiteSpace(dto.Ruc) && aliado.Ruc != dto.Ruc && await _repo.ExistePorRuc(dto.Ruc))
            return (false, "El RUC ya está en uso por otro aliado.");

        dto.ApplyTo(aliado);
        await _repo.Update(aliado);
        return (true, "Aliado actualizado correctamente.");
    }

    public async Task<(bool success, string mensaje)> Eliminar(int id)
    {
        var aliado = await _repo.GetById(id);
        if (aliado is null)
            return (false, "Aliado no encontrado.");

        await _repo.Delete(aliado);
        return (true, "Aliado eliminado correctamente.");
    }

    // -------------------------------------------------------------------------
    // Endpoint específico: activar / suspender aliado
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje)> CambiarEstado(int id, string nuevoEstado)
    {
        if (!EstadosValidos.Contains(nuevoEstado))
            return (false, $"Estado inválido. Valores permitidos: {string.Join(", ", EstadosValidos)}");

        var aliado = await _repo.GetById(id);
        if (aliado is null)
            return (false, "Aliado no encontrado.");

        aliado.Estado = nuevoEstado;
        await _repo.Update(aliado);
        return (true, $"Estado del aliado actualizado a '{nuevoEstado}'.");
    }
}
