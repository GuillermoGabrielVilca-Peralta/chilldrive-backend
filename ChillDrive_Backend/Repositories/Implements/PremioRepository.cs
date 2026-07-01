using ChillDrive_Backend.Models;
using ChillDrive_Backend.Models.Constants;
using Microsoft.EntityFrameworkCore;

namespace ChillDrive_Backend.Repositories.Implements;

public class PremioRepository : IPremioRepository
{
    private readonly ApplicationDbContext _context;

    public PremioRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Premio>> GetAll()
    {
        return await _context.Premios
            .Include(p => p.Aliado)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    public async Task<List<Premio>> GetByAliadoId(int aliadoId)
    {
        return await _context.Premios
            .Include(p => p.Aliado)
            .Where(p => p.AliadoId == aliadoId)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    public async Task<List<Premio>> GetDisponibles()
    {
        var hoy = FechaPeru.Hoy;
        return await _context.Premios
            .Include(p => p.Aliado)
            .Where(p => p.Estado == "Disponible" && p.Stock > 0
                && (p.FechaFin == null || p.FechaFin >= hoy)
                && (p.FechaInicio == null || p.FechaInicio <= hoy))
            .OrderBy(p => p.PuntosRequeridos)
            .ToListAsync();
    }

    public async Task<(List<Premio> items, int total)> GetDisponiblesPaginado(int skip, int take)
    {
        var hoy = FechaPeru.Hoy;
        // Vigencia igual que el canje (CanjeService): no mostrar premios vencidos o aún
        // no activos, para no ofrecer algo que luego el canje rechaza.
        var query = _context.Premios
            .Include(p => p.Aliado)
            .Where(p => p.Estado == "Disponible" && p.Stock > 0
                && (p.FechaFin == null || p.FechaFin >= hoy)
                && (p.FechaInicio == null || p.FechaInicio <= hoy))
            .OrderBy(p => p.PuntosRequeridos);

        var total = await query.CountAsync();
        var items = await query.Skip(skip).Take(take).ToListAsync();
        return (items, total);
    }

    public async Task<Premio?> GetById(int id)
    {
        return await _context.Premios
            .Include(p => p.Aliado)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<int> Add(Premio premio)
    {
        await _context.Premios.AddAsync(premio);
        await _context.SaveChangesAsync();
        return premio.Id;
    }

    public async Task Update(Premio premio)
    {
        _context.Premios.Update(premio);
        await _context.SaveChangesAsync();
    }

    public async Task Delete(Premio premio)
    {
        _context.Premios.Remove(premio);
        await _context.SaveChangesAsync();
    }
}
