namespace ChillDrive_Backend.Models.DTOs.Response;

/// <summary>
/// Envuelve cualquier lista paginada devuelta por la API.
/// </summary>
public class PaginadoDto<T>
{
    public List<T> Items        { get; init; } = [];
    public int     Total        { get; init; }
    public int     Pagina       { get; init; }
    public int     Tamano       { get; init; }
    public int     TotalPaginas { get; init; }
}
