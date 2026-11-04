using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Mappings;
using ChillDrive_Backend.Models.DTOs.Response;
using ChillDrive_Backend.Repositories;

namespace ChillDrive_Backend.Services.Implements;

// Rendimiento de la flota para el panel de administración: puntaje, infracciones,
// eficiencia (combustible estimado), zonas críticas y conductores a capacitar.
// Compara los últimos N días con los N días anteriores. Solo cuenta viajes Finalizados.
public class RendimientoService : IRendimientoService
{
    public static readonly int[] DiasPermitidos = [7, 30, 90];

    public const int PuntajeViajeSeguro = 80;
    // Requiere capacitación quien queda bajo este puntaje o lo baja en CaidaPuntajeAlerta.
    public const int PuntajeMinimo      = 60;
    public const int CaidaPuntajeAlerta = 5;
    public const int MaxZonasCriticas   = 200;

    private readonly IViajeRepository _viajeRepo;

    public RendimientoService(IViajeRepository viajeRepo)
    {
        _viajeRepo = viajeRepo;
    }

    public async Task<(bool success, string mensaje, RendimientoResponseDto? data)> Get(
        int dias, double kmPorGalon, double precioGalon)
    {
        if (!DiasPermitidos.Contains(dias))
            return (false, $"El periodo debe ser de {string.Join(", ", DiasPermitidos)} días.", null);
        if (kmPorGalon is < 5 or > 200)
            return (false, "El rendimiento del vehículo debe estar entre 5 y 200 km por galón.", null);
        if (precioGalon is < 1 or > 50)
            return (false, "El precio del galón debe estar entre S/ 1 y S/ 50.", null);

        var hasta = DateTime.UtcNow;
        var desde = hasta.AddDays(-dias);
        var viajes = await _viajeRepo.GetFinalizadosDesde(desde.AddDays(-dias));

        var actuales   = viajes.Where(v => v.FechaInicio >= desde).ToList();
        var anteriores = viajes.Where(v => v.FechaInicio <  desde).ToList();
        var eventos    = actuales.SelectMany(v => v.Eventos).ToList();
        var resumen    = Resumen(actuales);

        return (true, "OK", new RendimientoResponseDto
        {
            Dias          = dias,
            Desde         = desde,
            Hasta         = hasta,
            Actual        = resumen,
            Anterior      = Resumen(anteriores),
            Combustible   = Combustible(actuales, anteriores, kmPorGalon, precioGalon),
            Agrupacion    = dias > 30 ? "semana" : "dia",
            Tendencia     = Tendencia(actuales, semanal: dias > 30),
            PorTipo       = PorTipo(eventos, anteriores.SelectMany(v => v.Eventos)),
            PorHora       = PorHora(eventos),
            ZonasCriticas = ZonasCriticas(eventos),
            Conductores   = Conductores(actuales, anteriores, resumen.InfraccionesPor100Km)
        });
    }

    private static bool EsInfraccion(Evento e) => e.ImpactoPuntaje < 0;

    // Maniobras bruscas que gastan combustible de más (ver EficienciaCombustible).
    private static int Maniobras(IEnumerable<Viaje> viajes) => viajes
        .SelectMany(v => v.Eventos)
        .Count(e => EsInfraccion(e) && EficienciaCombustible.TiposQueConsumen.Contains(e.TipoEvento));

    private static double Km(IEnumerable<Viaje> viajes) => viajes.Sum(v => (double)(v.DistanciaKm ?? 0));

    private static double Por100Km(int cantidad, double km) => km > 0 ? Math.Round(cantidad / km * 100, 1) : 0;

    // El puntaje lo guarda Finalizar; si faltara, se recalcula con la misma fórmula.
    private static double Puntaje(Viaje v) => (double)(v.PuntajeConduccion
        ?? ScoreConduccion.General(v.Eventos.Select(e => (e.TipoEvento, e.ImpactoPuntaje))));

    private static double Sobreconsumo(IEnumerable<Viaje> viajes)
    {
        var lista = viajes as ICollection<Viaje> ?? viajes.ToList();
        return EficienciaCombustible.Sobreconsumo(Maniobras(lista), Km(lista));
    }

    private static DateOnly DiaPeru(DateTime utc) => DateOnly.FromDateTime(utc.AddHours(FechaPeru.OffsetHoras));

    private static ResumenRendimientoDto Resumen(List<Viaje> viajes)
    {
        var km           = Km(viajes);
        var infracciones = viajes.Sum(v => v.Eventos.Count(EsInfraccion));
        var sobreconsumo = Sobreconsumo(viajes);
        return new ResumenRendimientoDto
        {
            Viajes                  = viajes.Count,
            Conductores             = viajes.Select(v => v.UsuarioId).Distinct().Count(),
            Km                      = Math.Round(km, 1),
            Horas                   = Math.Round(viajes.Sum(v => v.DuracionMinutos ?? 0) / 60.0, 1),
            PuntajePromedio         = viajes.Count > 0 ? Math.Round(viajes.Average(Puntaje), 1) : null,
            Infracciones            = infracciones,
            InfraccionesPor100Km    = Por100Km(infracciones, km),
            PorcentajeViajesSeguros = viajes.Count > 0
                ? Math.Round(100.0 * viajes.Count(v => Puntaje(v) >= PuntajeViajeSeguro) / viajes.Count, 1)
                : null,
            IndiceEficiencia        = EficienciaCombustible.Indice(sobreconsumo),
            SobreconsumoPorcentaje  = Math.Round(sobreconsumo * 100, 1)
        };
    }

    private static CombustibleEstimadoDto Combustible(
        List<Viaje> actuales, List<Viaje> anteriores, double kmPorGalon, double precioGalon)
    {
        var galonesBase      = Km(actuales) / kmPorGalon;
        var sobreconsumo     = Sobreconsumo(actuales);
        var desperdiciados   = galonesBase * sobreconsumo;
        // Mismos km con el sobreconsumo del periodo anterior: lo que se dejó de gastar.
        var ahorrados        = Km(anteriores) > 0 ? galonesBase * (Sobreconsumo(anteriores) - sobreconsumo) : 0;
        return new CombustibleEstimadoDto
        {
            KmPorGalon            = kmPorGalon,
            PrecioGalon           = precioGalon,
            GalonesConsumidos     = Math.Round(galonesBase + desperdiciados, 1),
            GalonesDesperdiciados = Math.Round(desperdiciados, 1),
            SolesDesperdiciados   = Math.Round(desperdiciados * precioGalon, 2),
            GalonesAhorrados      = Math.Round(ahorrados, 1),
            SolesAhorrados        = Math.Round(ahorrados * precioGalon, 2)
        };
    }

    private static List<TendenciaRendimientoDto> Tendencia(List<Viaje> viajes, bool semanal)
    {
        DateOnly Tramo(Viaje v)
        {
            var dia = DiaPeru(v.FechaInicio);
            // Semanas de lunes a domingo.
            return semanal ? dia.AddDays(-(((int)dia.DayOfWeek + 6) % 7)) : dia;
        }

        return viajes
            .GroupBy(Tramo)
            .OrderBy(g => g.Key)
            .Select(g => new TendenciaRendimientoDto
            {
                Inicio               = g.Key,
                Viajes               = g.Count(),
                PuntajePromedio      = Math.Round(g.Average(Puntaje), 1),
                InfraccionesPor100Km = Por100Km(g.Sum(v => v.Eventos.Count(EsInfraccion)), Km(g))
            })
            .ToList();
    }

    private static List<EventoPorTipoDto> PorTipo(List<Evento> eventos, IEnumerable<Evento> anteriores)
    {
        var cantidadAnterior = anteriores.GroupBy(e => e.TipoEvento).ToDictionary(g => g.Key, g => g.Count());
        return eventos
            .GroupBy(e => e.TipoEvento)
            .Select(g => new EventoPorTipoDto
            {
                Tipo             = g.Key,
                EsInfraccion     = ImpactoEventos.Para(g.Key) < 0,
                Cantidad         = g.Count(),
                CantidadAnterior = cantidadAnterior.GetValueOrDefault(g.Key)
            })
            .OrderByDescending(t => t.Cantidad)
            .ToList();
    }

    private static List<EventosPorHoraDto> PorHora(List<Evento> eventos)
    {
        var porHora = eventos.ToLookup(e => e.FechaEvento.AddHours(FechaPeru.OffsetHoras).Hour);
        return Enumerable.Range(0, 24)
            .Select(h => new EventosPorHoraDto
            {
                Hora         = h,
                Eventos      = porHora[h].Count(),
                Infracciones = porHora[h].Count(EsInfraccion)
            })
            .ToList();
    }

    private static string TipoMasFrecuente(IEnumerable<Evento> eventos) => eventos
        .GroupBy(e => e.TipoEvento)
        .OrderByDescending(g => g.Count())
        .First().Key;

    // Agrupa las infracciones en celdas de 0,001° (~110 m) y devuelve las más cargadas.
    private static List<ZonaCriticaDto> ZonasCriticas(List<Evento> eventos) => eventos
        .Where(e => EsInfraccion(e) && e.Latitud.HasValue && e.Longitud.HasValue)
        .GroupBy(e => (Math.Round(e.Latitud!.Value, 3), Math.Round(e.Longitud!.Value, 3)))
        .Select(g => new ZonaCriticaDto
        {
            Latitud       = Math.Round(g.Average(e => e.Latitud!.Value), 6),
            Longitud      = Math.Round(g.Average(e => e.Longitud!.Value), 6),
            Infracciones  = g.Count(),
            TipoPrincipal = TipoMasFrecuente(g)
        })
        .OrderByDescending(z => z.Infracciones)
        .Take(MaxZonasCriticas)
        .ToList();

    private static List<ConductorRendimientoDto> Conductores(
        List<Viaje> actuales, List<Viaje> anteriores, double infraccionesFlotaPor100Km)
    {
        var anterioresPorConductor = anteriores.ToLookup(v => v.UsuarioId);
        return actuales
            .GroupBy(v => v.UsuarioId)
            .Select(g =>
            {
                var ultimo       = g.MaxBy(v => v.FechaInicio)!;
                var km           = Km(g);
                var infracciones = g.SelectMany(v => v.Eventos).Where(EsInfraccion).ToList();
                var previos      = anterioresPorConductor[g.Key].ToList();
                var puntaje      = Math.Round(g.Average(Puntaje), 1);
                double? anterior = previos.Count > 0 ? Math.Round(previos.Average(Puntaje), 1) : null;
                var por100Km     = Por100Km(infracciones.Count, km);
                return new ConductorRendimientoDto
                {
                    UsuarioId            = g.Key,
                    Conductor            = ultimo.Usuario.NombreCompleto(),
                    Placa                = ultimo.Vehiculo.Placa,
                    Viajes               = g.Count(),
                    Km                   = Math.Round(km, 1),
                    PuntajePromedio      = puntaje,
                    PuntajeAnterior      = anterior,
                    Infracciones         = infracciones.Count,
                    InfraccionesPor100Km = por100Km,
                    IndiceEficiencia     = EficienciaCombustible.Indice(Sobreconsumo(g)),
                    InfraccionPrincipal  = infracciones.Count > 0 ? TipoMasFrecuente(infracciones) : null,
                    MotivoCapacitacion   = MotivoCapacitacion(puntaje, anterior, por100Km, infraccionesFlotaPor100Km)
                };
            })
            .OrderByDescending(c => c.PuntajePromedio)
            .ToList();
    }

    private static string? MotivoCapacitacion(double puntaje, double? anterior, double por100Km, double flotaPor100Km)
    {
        if (puntaje < PuntajeMinimo)
            return $"Puntaje bajo ({puntaje:0})";
        if (anterior - puntaje >= CaidaPuntajeAlerta)
            return $"Bajó {anterior - puntaje:0} pts";
        if (flotaPor100Km > 0 && por100Km >= 2 * flotaPor100Km)
            return "El doble de infracciones que la flota";
        return null;
    }
}
