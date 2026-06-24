namespace ChillDrive_Backend.Models.DTOs.Response;

/// <summary>
/// Fila del leaderboard de conductores (vista v_ranking_conductores): combina saldo, nivel,
/// puntaje promedio de conducción y total de viajes para mostrar un ranking enriquecido.
/// </summary>
public class RankingConductorResponseDto
{
    public int     UsuarioId        { get; set; }
    public string  Conductor        { get; set; } = null!;
    public string  TipoConductor    { get; set; } = null!;
    public int     PuntosActuales   { get; set; }
    public int     PuntosHistoricos { get; set; }
    public string  Nivel            { get; set; } = null!;
    public decimal PuntajePromedio  { get; set; }
    public long    TotalViajes      { get; set; }
}
