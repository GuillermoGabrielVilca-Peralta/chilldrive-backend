using ChillDrive_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace ChillDrive_Backend.Repositories.Implements;

public class MovimientoPuntoRepository : IMovimientoPuntoRepository
{
    private readonly ApplicationDbContext _context;

    public MovimientoPuntoRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<MovimientosPunto>> GetByUsuarioId(int usuarioId)
    {
        return await _context.MovimientosPuntos
            .Include(m => m.Usuario)
            .Where(m => m.UsuarioId == usuarioId)
            .OrderByDescending(m => m.FechaMovimiento)
            .ToListAsync();
    }

    public async Task<List<MovimientosPunto>> GetByUsuarioIdYTipo(int usuarioId, string tipo)
    {
        return await _context.MovimientosPuntos
            .Include(m => m.Usuario)
            .Where(m => m.UsuarioId == usuarioId && m.Tipo == tipo)
            .OrderByDescending(m => m.FechaMovimiento)
            .ToListAsync();
    }

    public async Task<MovimientosPunto?> GetById(int id)
    {
        return await _context.MovimientosPuntos
            .Include(m => m.Usuario)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<int> Add(MovimientosPunto movimiento)
    {
        await _context.MovimientosPuntos.AddAsync(movimiento);
        await _context.SaveChangesAsync();
        return movimiento.Id;
    }
}
