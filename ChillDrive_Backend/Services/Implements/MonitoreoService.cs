using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Mappings;
using ChillDrive_Backend.Models.DTOs.Response;
using ChillDrive_Backend.Repositories;

namespace ChillDrive_Backend.Services.Implements;

// Monitoreo en vivo de la flota para el panel de administración.
public class MonitoreoService : IMonitoreoService
{
    // Un viaje "En Curso" que lleva más de esta ventana sin enviar posición se da por
    // inactivo (app cerrada, sin señal prolongada o viaje olvidado) y no se muestra en el mapa.
    public const int VentanaActividadMinutos = 10;

    private readonly IViajeRepository _viajeRepo;

    public MonitoreoService(IViajeRepository viajeRepo)
    {
        _viajeRepo = viajeRepo;
    }

    public async Task<List<ViajeActivoResponseDto>> GetViajesActivos()
    {
        var ahora   = DateTime.UtcNow;
        var viajes  = await _viajeRepo.GetActivosConUbicacion(ahora.AddMinutes(-VentanaActividadMinutos));

        return viajes.Select(v =>
        {
            // Mismo cálculo que ViajeService.Finalizar: el puntaje en vivo coincide con el final.
            var puntaje   = ScoreConduccion.General(v.Eventos.Select(e => (e.TipoEvento, e.ImpactoPuntaje)));
            var segundos  = (int)Math.Max(0, (ahora - v.UltimaUbicacionFecha!.Value).TotalSeconds);
            return v.ToViajeActivo(puntaje, segundos);
        }).ToList();
    }
}
