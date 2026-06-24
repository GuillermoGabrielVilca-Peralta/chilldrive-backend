namespace ChillDrive_Backend.Models.DTOs.Response;

public class AliadoResponseDto
{
    public int      Id                  { get; set; }
    public string   Nombre              { get; set; } = null!;
    public string?  Ruc                 { get; set; }
    public string   Categoria           { get; set; } = null!;
    public string?  Direccion           { get; set; }
    public string?  Distrito            { get; set; }
    public string?  Telefono            { get; set; }
    public string?  Email               { get; set; }
    public string?  Logo                { get; set; }
    public string?  Descripcion         { get; set; }
    public decimal  ComisionPorcentaje  { get; set; }
    public string   Estado              { get; set; } = null!;
    public DateTime FechaRegistro       { get; set; }
}