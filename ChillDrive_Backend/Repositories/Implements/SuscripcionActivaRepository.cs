using ChillDrive_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace ChillDrive_Backend.Repositories.Implements;

public class SuscripcionActivaRepository : ISuscripcionActivaRepository
{
    private readonly ApplicationDbContext _context;

    public SuscripcionActivaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Lee la vista ordenada por días restantes (las que vencen antes, primero).
    public async Task<List<VSuscripcionesActiva>> GetActivas()
    {
        return await _context.VSuscripcionesActivas
            .OrderBy(v => v.DiasRestantes)
            .ToListAsync();
    }
}
