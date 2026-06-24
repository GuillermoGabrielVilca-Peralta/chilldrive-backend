using ChillDrive_Backend.Models.DTOs.Mappings;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;
using ChillDrive_Backend.Repositories;

namespace ChillDrive_Backend.Services.Implements;

public class ViajeService : IViajeService
{
    private readonly IViajeRepository    _repo;
    private readonly IUsuarioRepository  _usuarioRepo;
    private readonly IVehiculoRepository _vehiculoRepo;
    private readonly IEventoRepository   _eventoRepo;

    // IEventoRepository se usa al finalizar para calcular el neto de puntos del viaje
    // (los puntos ya se acreditaron evento por evento; aquí solo se totaliza para el viaje).
    public ViajeService(
        IViajeRepository    repo,
        IUsuarioRepository  usuarioRepo,
        IVehiculoRepository vehiculoRepo,
        IEventoRepository   eventoRepo)
    {
        _repo         = repo;
        _usuarioRepo  = usuarioRepo;
        _vehiculoRepo = vehiculoRepo;
        _eventoRepo   = eventoRepo;
    }

    public async Task<List<ViajeResponseDto>> GetAll()
    {
        var lista = await _repo.GetAll();
        return lista.Select(v => v.ToResponse()).ToList();
    }

    public async Task<List<ViajeResponseDto>> GetByUsuarioId(int usuarioId)
    {
        var lista = await _repo.GetByUsuarioId(usuarioId);
        return lista.Select(v => v.ToResponse()).ToList();
    }

    public async Task<PaginadoDto<ViajeResponseDto>> GetByUsuarioIdPaginado(int usuarioId, int pagina, int tamano)
    {
        var skip = (pagina - 1) * tamano;
        var (items, total) = await _repo.GetByUsuarioIdPaginado(usuarioId, skip, tamano);
        return new PaginadoDto<ViajeResponseDto>
        {
            Items        = items.Select(v => v.ToResponse()).ToList(),
            Total        = total,
            Pagina       = pagina,
            Tamano       = tamano,
            TotalPaginas = (int)Math.Ceiling((double)total / tamano)
        };
    }

    public async Task<List<ViajeResponseDto>> GetByVehiculoId(int vehiculoId)
    {
        var lista = await _repo.GetByVehiculoId(vehiculoId);
        return lista.Select(v => v.ToResponse()).ToList();
    }

    public async Task<ViajeResponseDto?> GetById(int id)
    {
        var v = await _repo.GetById(id);
        return v?.ToResponse();
    }

    // -------------------------------------------------------------------------
    // INICIAR VIAJE — crea el viaje en estado "En Curso"
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje, int? id)> Iniciar(ViajeRequestDto dto)
    {
        var usuario = await _usuarioRepo.GetById(dto.UsuarioId);
        if (usuario is null)
            return (false, "El usuario no existe.", null);

        var vehiculo = await _vehiculoRepo.GetById(dto.VehiculoId);
        if (vehiculo is null)
            return (false, "El vehículo no existe.", null);

        // Verificar que el vehículo pertenece al usuario
        if (vehiculo.UsuarioId != dto.UsuarioId)
            return (false, "El vehículo no pertenece al usuario.", null);

        var id = await _repo.Add(dto.ToEntity());
        return (true, "Viaje iniciado correctamente.", id);
    }

    // -------------------------------------------------------------------------
    // FINALIZAR VIAJE — endpoint específico de negocio.
    // Los puntos del conductor YA se ajustaron en tiempo real con cada evento durante el
    // viaje (ver EventoService), así que aquí NO se acreditan de nuevo (sería doble conteo).
    // Solo se cierra el viaje, se guarda el puntaje resumen de la IA y el neto de puntos
    // del viaje (suma de los impactos de sus eventos), como dato informativo.
    // -------------------------------------------------------------------------
    private const int DuracionMinimaMinutos = 5;

    public async Task<(bool success, string mensaje)> Finalizar(int id, FinalizarViajeRequestDto dto)
    {
        var viaje = await _repo.GetById(id);
        if (viaje is null)
            return (false, "Viaje no encontrado.");

        if (viaje.Estado != "En Curso")
            return (false, $"El viaje ya está en estado '{viaje.Estado}' y no puede finalizarse.");

        // Viaje muy corto → cancelar automáticamente en lugar de registrar.
        var duracion = dto.DuracionMinutos;
        if (duracion < DuracionMinimaMinutos)
        {
            viaje.Estado = "Cancelado";
            await _repo.Update(viaje);
            return (false, $"El viaje duró solo {duracion} min (mínimo {DuracionMinimaMinutos} min). Fue cancelado automáticamente y no cuenta para retos ni racha.");
        }

        // Neto de puntos del viaje = suma de los impactos de sus eventos (puede ser negativo).
        var eventos     = await _eventoRepo.GetByViajeId(id);
        var puntosNetos = eventos.Sum(e => e.ImpactoPuntaje);

        viaje.FechaFin          = dto.FechaFin;
        viaje.DistanciaKm       = dto.DistanciaKm;
        viaje.DuracionMinutos   = dto.DuracionMinutos;
        viaje.PuntajeConduccion = dto.PuntajeConduccion;
        viaje.PuntosGanados     = puntosNetos;
        viaje.Estado            = "Finalizado";

        await _repo.Update(viaje);
        return (true, $"Viaje finalizado. Duración: {duracion} min. Puntos del viaje: {puntosNetos}.");
    }

    // -------------------------------------------------------------------------
    // CANCELAR VIAJE
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje)> Cancelar(int id)
    {
        var viaje = await _repo.GetById(id);
        if (viaje is null)
            return (false, "Viaje no encontrado.");

        if (viaje.Estado != "En Curso")
            return (false, $"Solo se pueden cancelar viajes en curso. Estado actual: '{viaje.Estado}'.");

        viaje.Estado = "Cancelado";
        await _repo.Update(viaje);
        return (true, "Viaje cancelado correctamente.");
    }

    // -------------------------------------------------------------------------
    // ELIMINAR
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje)> Eliminar(int id)
    {
        var viaje = await _repo.GetById(id);
        if (viaje is null)
            return (false, "Viaje no encontrado.");

        await _repo.Delete(viaje);
        return (true, "Viaje eliminado correctamente.");
    }
}
