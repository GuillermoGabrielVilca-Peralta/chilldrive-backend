namespace ChillDrive_Backend.Models.DTOs.Response;

// Un vehículo en ruta tal como lo dibuja el panel de monitoreo en el mapa.
public class ViajeActivoResponseDto
{
    public int       ViajeId                    { get; set; }
    public int       UsuarioId                  { get; set; }
    public string    Conductor                  { get; set; } = null!;
    public string    Placa                      { get; set; } = null!;
    public string?   Vehiculo                   { get; set; }
    public DateTime  FechaInicio                { get; set; }
    public decimal   Latitud                    { get; set; }
    public decimal   Longitud                   { get; set; }
    public decimal?  VelocidadKmh               { get; set; }

    // Calculado en el servidor: evita depender de la zona horaria del navegador
    // (las fechas se guardan como timestamp sin zona en UTC).
    public int       SegundosDesdeUltimaUbicacion { get; set; }

    // Puntaje 0–100 del viaje hasta el momento (misma fórmula que al finalizar).
    public int       PuntajeActual              { get; set; }

    public List<EventoMapaResponseDto> Eventos  { get; set; } = new();
}

// Evento del viaje con coordenadas, para marcarlo en el mapa.
public class EventoMapaResponseDto
{
    public string    TipoEvento     { get; set; } = null!;
    public int       ImpactoPuntaje { get; set; }
    public decimal   Latitud        { get; set; }
    public decimal   Longitud       { get; set; }
    public DateTime  FechaEvento    { get; set; }
}
