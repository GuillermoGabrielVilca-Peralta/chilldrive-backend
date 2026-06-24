namespace ChillDrive_Backend.Models.Constants;

public static class NivelPuntos
{
    public static string Calcular(int puntosHistoricos) => puntosHistoricos switch
    {
        >= 5000 => "Diamante",
        >= 2000 => "Platino",
        >= 800  => "Oro",
        >= 300  => "Plata",
        _       => "Bronce"
    };
}
