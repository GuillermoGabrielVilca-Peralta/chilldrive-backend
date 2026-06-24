using ChillDrive_Backend.Models;
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
            .OrderByDescending(s => s.FechaInicio)
            .ToListAsync();
    }

    public async Task<Suscripcione?> GetById(int id)
    {
        return await _context.Suscripciones
            .Include(s => s.Usuario)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Suscripcione?> GetActivaByUsuarioId(int usuarioId)
    {
        return await _context.Suscripciones
            .Where(s => s.UsuarioId == usuarioId && s.Estado == "Activa")
            .OrderByDescending(s => s.FechaFin)
            .FirstOrDefaultAsync();
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