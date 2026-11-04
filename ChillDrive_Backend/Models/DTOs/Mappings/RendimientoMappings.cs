namespace ChillDrive_Backend.Models.DTOs.Mappings;

public static class RendimientoMappings
{
    // "Ana Torres", como lo muestran el mapa y el ranking del panel.
    public static string NombreCompleto(this Usuario usuario) =>
        $"{usuario.Nombres} {usuario.Apellidos}".Trim();
}
