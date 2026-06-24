using ChillDrive_Backend.Models;

namespace ChillDrive_Backend.Repositories;

public interface IPuntosUsuarioRepository
{
    Task<PuntosUsuario?>      GetByUsuarioId(int usuarioId);
    Task                      AgregarOActualizar(int usuarioId, int puntosASumar);
    Task                      DescontarPuntos(int usuarioId, int puntosADescontar);

    /// <summary>
    /// Aplica el impacto (con signo) de un evento al puntaje del conductor: el saldo no baja
    /// de 0 y el histórico solo crece con impactos positivos. Hace upsert si no hay registro.
    /// </summary>
    Task                      AplicarImpactoEvento(int usuarioId, int delta);
}
