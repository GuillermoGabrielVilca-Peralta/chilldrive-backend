namespace ChillDrive_Backend.Models.DTOs.Response;

public class EstadisticasConduccionDto
{
    public int ScoreGeneral { get; set; }
    public int TotalEventosNegativos { get; set; }
    public List<ScoreCategoriaDto> Categorias { get; set; } = [];
}

public class ScoreCategoriaDto
{
    public string Nombre { get; set; } = null!;
    public int Score { get; set; }
    public int Cantidad { get; set; }
}
