using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.Constants;
using Microsoft.EntityFrameworkCore;

namespace ChillDrive_Backend.Repositories.Implements;

public class PuntosUsuarioRepository : IPuntosUsuarioRepository
{
    private readonly ApplicationDbContext _context;

    public PuntosUsuarioRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PuntosUsuario?> GetByUsuarioId(int usuarioId)
    {
        return await _context.PuntosUsuarios
            .Include(p => p.Usuario)
            .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);
    }

    // Upsert: si no existe el registro lo crea, si existe suma los puntos
    public async Task AgregarOActualizar(int usuarioId, int puntosASumar)
    {
        var registro = await _context.PuntosUsuarios
            .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

        if (registro is null)
        {
            registro = new PuntosUsuario
            {
                UsuarioId           = usuarioId,
                PuntosActuales      = puntosASumar,
                PuntosHistoricos    = puntosASumar,
                Nivel               = NivelPuntos.Calcular(puntosASumar),
                UltimaActualizacion = DateTime.UtcNow
            };
            await _context.PuntosUsuarios.AddAsync(registro);
        }
        else
        {
            registro.PuntosActuales     += puntosASumar;
            registro.PuntosHistoricos   += puntosASumar;
            registro.Nivel               = NivelPuntos.Calcular(registro.PuntosHistoricos);
            registro.UltimaActualizacion = DateTime.UtcNow;
            _context.PuntosUsuarios.Update(registro);
        }

        await _context.SaveChangesAsync();
    }

    public async Task DescontarPuntos(int usuarioId, int puntosADescontar)
    {
        var registro = await _context.PuntosUsuarios
            .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

        // El servicio garantiza que el usuario existe y tiene puntos suficientes
        // antes de llegar aquí; esta operación solo persiste el descuento.
        registro!.PuntosActuales    -= puntosADescontar;
        registro.UltimaActualizacion = DateTime.UtcNow;
        _context.PuntosUsuarios.Update(registro);
        await _context.SaveChangesAsync();
    }

    // Aplica el impacto (con signo) de un evento. A diferencia de los otros métodos, acepta
    // delta negativo: el saldo (PuntosActuales) se topa en 0 y el histórico solo sube con
    // impactos positivos (las infracciones no reducen el histórico ni el nivel alcanzado).
    public async Task AplicarImpactoEvento(int usuarioId, int delta)
    {
        var registro = await _context.PuntosUsuarios
            .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

        if (registro is null)
        {
            var inicial = Math.Max(0, delta);   // un primer evento negativo deja el saldo en 0
            registro = new PuntosUsuario
            {
                UsuarioId           = usuarioId,
                PuntosActuales      = inicial,
                PuntosHistoricos    = inicial,
                Nivel               = NivelPuntos.Calcular(inicial),
                UltimaActualizacion = DateTime.UtcNow
            };
            await _context.PuntosUsuarios.AddAsync(registro);
        }
        else
        {
            registro.PuntosActuales = Math.Max(0, registro.PuntosActuales + delta);
            if (delta > 0)
                registro.PuntosHistoricos += delta;
            registro.Nivel               = NivelPuntos.Calcular(registro.PuntosHistoricos);
            registro.UltimaActualizacion = DateTime.UtcNow;
            _context.PuntosUsuarios.Update(registro);
        }

        await _context.SaveChangesAsync();
    }
}
