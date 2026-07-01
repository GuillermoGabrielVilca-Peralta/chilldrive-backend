using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.Constants;
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

    // Perú no tiene horario de verano: UTC-5 fijo. Se usa para decidir si un viaje
    // abierto es "de hoy" (se retoma) o de un día anterior (se cierra).
    private const int OffsetPeruHoras = -5;

    // -------------------------------------------------------------------------
    // INICIAR VIAJE — un usuario solo puede tener UN viaje activo a la vez.
    // Si ya tiene uno abierto:
    //   - del mismo día (hora Perú) → se RETOMA ese viaje (no se crea otro).
    //   - de un día anterior        → se CIERRA como Finalizado y se crea uno nuevo.
    // Así nunca quedan varios "En Curso" colgados (p. ej. si la app se cerró sin
    // finalizar), y un viaje puede pausarse y continuarse durante el día.
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

        // Resolver el "un solo viaje activo".
        var abiertos = await _repo.GetEnCursoByUsuario(dto.UsuarioId);
        if (abiertos.Count > 0)
        {
            var hoy = DiaEnPeru(DateTime.UtcNow);
            // El más reciente de hoy (la lista viene del más nuevo al más viejo).
            var deHoy = abiertos.FirstOrDefault(v => DiaEnPeru(v.FechaInicio) == hoy);

            // Cerrar todos los demás (días anteriores y duplicados extra del mismo día).
            foreach (var viejo in abiertos.Where(v => v.Id != deHoy?.Id))
                await CerrarViajeAbandonado(viejo);

            if (deHoy is not null)
                return (true, "Se retomó tu viaje en curso de hoy.", deHoy.Id);
        }

        var id = await _repo.Add(dto.ToEntity());
        return (true, "Viaje iniciado correctamente.", id);
    }

    // Cierra un viaje que quedó abierto (de un día anterior o duplicado): lo marca como
    // Finalizado con el neto de puntos de sus eventos. Los puntos del conductor ya se
    // aplicaron evento por evento, así que esto solo deja el viaje consistente en el historial.
    private async Task CerrarViajeAbandonado(Viaje viaje)
    {
        var eventos = await _eventoRepo.GetByViajeId(viaje.Id);
        // Nunca negativo: el constraint chk_viajes_puntos exige puntos_ganados >= 0
        // (las infracciones ya se descontaron del saldo del conductor evento por evento).
        viaje.PuntosGanados = Math.Max(0, eventos.Sum(e => e.ImpactoPuntaje));
        viaje.FechaFin      = DateTime.UtcNow;
        viaje.Estado        = "Finalizado";
        await _repo.Update(viaje);
    }

    // Fecha calendario en hora de Perú (UTC-5) de un instante UTC.
    private static DateOnly DiaEnPeru(DateTime utc) =>
        DateOnly.FromDateTime(utc.AddHours(OffsetPeruHoras));

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

        // Neto de puntos del viaje = suma de los impactos de sus eventos. Se guarda
        // limitado a 0 (el constraint chk_viajes_puntos exige puntos_ganados >= 0); las
        // infracciones ya se descontaron del saldo del conductor evento por evento.
        var eventos     = await _eventoRepo.GetByViajeId(id);
        var puntosNetos = Math.Max(0, eventos.Sum(e => e.ImpactoPuntaje));

        // Puntaje 0–100 de la IA calculado en el SERVIDOR desde los eventos del viaje
        // (misma fórmula que EventoService y la app). Se ignora `dto.PuntajeConduccion`
        // (el cliente enviaba 100 fijo): el puntaje no debe ser alterable desde la app.
        var puntajeConduccion = ScoreConduccion.General(
            eventos.Select(e => (e.TipoEvento, e.ImpactoPuntaje)));

        viaje.FechaFin          = dto.FechaFin;
        viaje.DistanciaKm       = dto.DistanciaKm;
        viaje.DuracionMinutos   = dto.DuracionMinutos;
        viaje.PuntajeConduccion = puntajeConduccion;
        viaje.PuntosGanados     = puntosNetos;
        viaje.Estado            = "Finalizado";

        await _repo.Update(viaje);
        return (true, $"Viaje finalizado. Duración: {duracion} min. Puntaje: {puntajeConduccion}/100. Puntos del viaje: {puntosNetos}.");
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
