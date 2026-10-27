using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Models.DTOs.Mappings;

public static class MonitoreoMappings
{
    // Requiere Usuario, Vehiculo y Eventos cargados (ver IViajeRepository.GetActivosConUbicacion).
    // `puntajeActual` y `segundosDesdeUltimaUbicacion` los calcula el servicio.
    public static ViajeActivoResponseDto ToViajeActivo(
        this Viaje v, int puntajeActual, int segundosDesdeUltimaUbicacion) => new()
    {
        ViajeId                      = v.Id,
        UsuarioId                    = v.UsuarioId,
        Conductor                    = $"{v.Usuario.Nombres} {v.Usuario.Apellidos}".Trim(),
        Placa                        = v.Vehiculo.Placa,
        Vehiculo                     = NombreVehiculo(v.Vehiculo),
        FechaInicio                  = v.FechaInicio,
        Latitud                      = v.UltimaLatitud  ?? 0,
        Longitud                     = v.UltimaLongitud ?? 0,
        VelocidadKmh                 = v.UltimaVelocidadKmh,
        SegundosDesdeUltimaUbicacion = segundosDesdeUltimaUbicacion,
        PuntajeActual                = puntajeActual,
        // Solo los eventos con coordenadas se pueden dibujar en el mapa.
        Eventos = v.Eventos
            .Where(e => e.Latitud.HasValue && e.Longitud.HasValue)
            .OrderBy(e => e.FechaEvento)
            .Select(e => new EventoMapaResponseDto
            {
                TipoEvento     = e.TipoEvento,
                ImpactoPuntaje = e.ImpactoPuntaje,
                Latitud        = e.Latitud!.Value,
                Longitud       = e.Longitud!.Value,
                FechaEvento    = e.FechaEvento
            })
            .ToList()
    };

    // "Toyota Yaris"; null si el vehículo no tiene marca ni modelo registrados.
    private static string? NombreVehiculo(Vehiculo vehiculo)
    {
        var nombre = $"{vehiculo.Marca} {vehiculo.Modelo}".Trim();
        return nombre.Length > 0 ? nombre : null;
    }
}
