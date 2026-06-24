using ChillDrive_Backend.Models.DTOs.Mappings;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;
using ChillDrive_Backend.Repositories;

namespace ChillDrive_Backend.Services.Implements;

public class PremioService : IPremioService
{
    private readonly IPremioRepository  _repo;
    private readonly IAliadoRepository  _aliadoRepo;

    private static readonly string[] EstadosValidos = ["Disponible", "Agotado", "Vencido", "Pausado"];

    public PremioService(IPremioRepository repo, IAliadoRepository aliadoRepo)
    {
        _repo       = repo;
        _aliadoRepo = aliadoRepo;
    }

    public async Task<List<PremioResponseDto>> GetAll()
    {
        var lista = await _repo.GetAll();
        return lista.Select(p => p.ToResponse()).ToList();
    }

    public async Task<List<PremioResponseDto>> GetByAliadoId(int aliadoId)
    {
        var lista = await _repo.GetByAliadoId(aliadoId);
        return lista.Select(p => p.ToResponse()).ToList();
    }

    public async Task<List<PremioResponseDto>> GetDisponibles()
    {
        var lista = await _repo.GetDisponibles();
        return lista.Select(p => p.ToResponse()).ToList();
    }

    public async Task<PaginadoDto<PremioResponseDto>> GetDisponiblesPaginado(int pagina, int tamano)
    {
        var skip = (pagina - 1) * tamano;
        var (items, total) = await _repo.GetDisponiblesPaginado(skip, tamano);
        return new PaginadoDto<PremioResponseDto>
        {
            Items        = items.Select(p => p.ToResponse()).ToList(),
            Total        = total,
            Pagina       = pagina,
            Tamano       = tamano,
            TotalPaginas = (int)Math.Ceiling((double)total / tamano)
        };
    }

    public async Task<PremioResponseDto?> GetById(int id)
    {
        var p = await _repo.GetById(id);
        return p?.ToResponse();
    }

    // -------------------------------------------------------------------------
    // REGISTRAR
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje, int? id)> Registrar(PremioRequestDto dto)
    {
        var aliado = await _aliadoRepo.GetById(dto.AliadoId);
        if (aliado is null)
            return (false, "El aliado no existe.", null);

        if (aliado.Estado != "Activo")
            return (false, "No se pueden registrar premios para un aliado inactivo o suspendido.", null);

        // PuntosRequeridos (>=1) y Stock (>=0) ya los valida [Range] en PremioRequestDto.
        // Aquí solo queda la regla de campos cruzados que el DTO no puede expresar:
        if (dto.FechaFin.HasValue && dto.FechaInicio.HasValue && dto.FechaFin < dto.FechaInicio)
            return (false, "La fecha de fin no puede ser anterior a la fecha de inicio.", null);

        var id = await _repo.Add(dto.ToEntity());
        return (true, "Premio registrado correctamente.", id);
    }

    // -------------------------------------------------------------------------
    // ACTUALIZAR
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje)> Actualizar(int id, PremioRequestDto dto)
    {
        var premio = await _repo.GetById(id);
        if (premio is null)
            return (false, "Premio no encontrado.");

        var aliado = await _aliadoRepo.GetById(dto.AliadoId);
        if (aliado is null)
            return (false, "El aliado no existe.");

        // PuntosRequeridos (>=1) y Stock (>=0) ya los valida [Range] en PremioRequestDto.
        // Aquí solo queda la regla de campos cruzados que el DTO no puede expresar:
        if (dto.FechaFin.HasValue && dto.FechaInicio.HasValue && dto.FechaFin < dto.FechaInicio)
            return (false, "La fecha de fin no puede ser anterior a la fecha de inicio.");

        dto.ApplyTo(premio);
        await _repo.Update(premio);
        return (true, "Premio actualizado correctamente.");
    }

    // -------------------------------------------------------------------------
    // ELIMINAR
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje)> Eliminar(int id)
    {
        var premio = await _repo.GetById(id);
        if (premio is null)
            return (false, "Premio no encontrado.");

        await _repo.Delete(premio);
        return (true, "Premio eliminado correctamente.");
    }

    // -------------------------------------------------------------------------
    // Endpoint específico: pausar / activar / marcar agotado
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje)> CambiarEstado(int id, string nuevoEstado)
    {
        if (!EstadosValidos.Contains(nuevoEstado))
            return (false, $"Estado inválido. Valores permitidos: {string.Join(", ", EstadosValidos)}");

        var premio = await _repo.GetById(id);
        if (premio is null)
            return (false, "Premio no encontrado.");

        premio.Estado = nuevoEstado;
        await _repo.Update(premio);
        return (true, $"Estado del premio actualizado a '{nuevoEstado}'.");
    }
}
