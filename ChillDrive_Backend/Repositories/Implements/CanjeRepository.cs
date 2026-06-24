using ChillDrive_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace ChillDrive_Backend.Repositories.Implements;

public class CanjeRepository : ICanjeRepository
{
    private readonly ApplicationDbContext _context;

    public CanjeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Canje>> GetAll()
    {
        return await _context.Canjes
            .Include(c => c.Premio)
            .ThenInclude(p => p.Aliado)
            .Include(c => c.Usuario)
            .OrderByDescending(c => c.FechaCanje)
            .ToListAsync();
    }

    public async Task<List<Canje>> GetByUsuarioId(int usuarioId)
    {
        return await _context.Canjes
            .Include(c => c.Premio)
            .ThenInclude(p => p.Aliado)
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.FechaCanje)
            .ToListAsync();
    }

    public async Task<Canje?> GetById(int id)
    {
        return await _context.Canjes
            .Include(c => c.Premio)
            .ThenInclude(p => p.Aliado)
            .Include(c => c.Usuario)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Canje?> GetByCodigoQr(string codigoQr)
    {
        return await _context.Canjes
            .Include(c => c.Premio)
            .ThenInclude(p => p.Aliado)
            .Include(c => c.Usuario)
            .FirstOrDefaultAsync(c => c.CodigoQr == codigoQr);
    }

    public async Task<int> Add(Canje canje)
    {
        await _context.Canjes.AddAsync(canje);
        await _context.SaveChangesAsync();
        return canje.Id;
    }

    public async Task Update(Canje canje)
    {
        _context.Canjes.Update(canje);
        await _context.SaveChangesAsync();
    }
}
