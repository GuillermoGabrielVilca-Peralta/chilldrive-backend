using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.Constants;
using Microsoft.EntityFrameworkCore;

namespace ChillDrive_Backend.Repositories.Implements;

public class SuscripcionRepository : ISuscripcionRepository
{
    private readonly ApplicationDbContext _context;

    public SuscripcionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Suscripcione>> GetAll()
    {
        return await _context.Suscripciones
            .Include(s => s.Usuario)
            .OrderByDescending(s => s.FechaRegistro)
            .ToListAsync();
    }

    public async Task<List<Suscripcione>> GetByUsuarioId(int usuarioId)
    {
        return await _context.Suscripciones
            .Where(s => s.UsuarioId == usuarioId)
            .OrderByDescending(s => s.FechaRegistro)
            .ToListAsync();
    }

    public async Task<Suscripcione?> GetById(int id)
    {
        return await _context.Suscripciones
            .Include(s => s.Usuario)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    // La de fin más lejano: si hay una renovación encadenada, es la que marca el vencimiento.
    public async Task<Suscripcione?> GetActivaByUsuarioId(int usuarioId)
    {
        return await _context.Suscripciones
            .Where(s => s.UsuarioId == usuarioId && s.Estado == PlanesSuscripcion.Activa)
            .OrderByDescending(s => s.FechaFin)
            .FirstOrDefaultAsync();
    }

    public async Task<Suscripcione?> GetPendienteByUsuarioId(int usuarioId)
    {
        return await _context.Suscripciones
            .FirstOrDefaultAsync(s => s.UsuarioId == usuarioId && s.Estado == PlanesSuscripcion.Pendiente);
    }

    // Más antiguas primero: el administrador las atiende en orden de llegada.
    public async Task<List<Suscripcione>> GetPendientes()
    {
        return await _context.Suscripciones
            .Include(s => s.Usuario)
            .Where(s => s.Estado == PlanesSuscripcion.Pendiente)
            .OrderBy(s => s.FechaRegistro)
            .ToListAsync();
    }

    public async Task<bool> UsoPrueba(int usuarioId)
    {
        return await _context.Suscripciones
            .AnyAsync(s => s.UsuarioId == usuarioId && s.Plan == PlanesSuscripcion.Prueba);
    }

    public async Task<bool> ExisteReferenciaYape(string referencia)
    {
        return await _context.Suscripciones
            .AnyAsync(s => s.MetodoPago == PlanesSuscripcion.Yape && s.ReferenciaPago == referencia);
    }

    public async Task<bool> ExisteCodigoPago(string codigo)
    {
        return await _context.Suscripciones.AnyAsync(s => s.CodigoPago == codigo);
    }

    public async Task<List<Suscripcione>> GetActivasVencidas(DateOnly hoy)
    {
        return await _context.Suscripciones
            .Where(s => s.Estado == PlanesSuscripcion.Activa && s.FechaFin <= hoy)
            .ToListAsync();
    }

    public async Task<bool> TieneVigente(int usuarioId, DateOnly hoy)
    {
        return await _context.Suscripciones
            .AnyAsync(s => s.UsuarioId == usuarioId
                        && s.Estado == PlanesSuscripcion.Activa
                        && s.FechaFin > hoy);
    }

    public async Task<int> Add(Suscripcione suscripcion)
    {
        await _context.Suscripciones.AddAsync(suscripcion);
        await _context.SaveChangesAsync();
        return suscripcion.Id;
    }

    public async Task Update(Suscripcione suscripcion)
    {
        _context.Suscripciones.Update(suscripcion);
        await _context.SaveChangesAsync();
    }
}
