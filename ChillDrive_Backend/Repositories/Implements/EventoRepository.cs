using ChillDrive_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace ChillDrive_Backend.Repositories.Implements;

public class EventoRepository : IEventoRepository
{
    private readonly ApplicationDbContext _context;

    public EventoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Evento>> GetAll()
    {
        return await _context.Eventos
            .OrderByDescending(e => e.FechaEvento)
            .ToListAsync();
    }

    public async Task<List<Evento>> GetByViajeId(int viajeId)
    {
        return await _context.Eventos
            .Where(e => e.ViajeId == viajeId)
            .OrderBy(e => e.FechaEvento)
            .ToListAsync();
    }

    public async Task<(List<Evento> items, int total)> GetByViajeIdPaginado(int viajeId, int skip, int take)
    {
        var query = _context.Eventos
            .Where(e => e.ViajeId == viajeId)
            .OrderBy(e => e.FechaEvento);

        var total = await query.CountAsync();
        var items = await query.Skip(skip).Take(take).ToListAsync();
        return (items, total);
    }

    public async Task<List<Evento>> GetByCamaraId(int camaraId)
    {
        return await _context.Eventos
            .Where(e => e.CamaraId == camaraId)
            .OrderByDescending(e => e.FechaEvento)
            .ToListAsync();
    }

    public async Task<Evento?> GetById(int id)
    {
        return await _context.Eventos.FindAsync(id);
    }

    public async Task<int> Add(Evento evento)
    {
        await _context.Eventos.AddAsync(evento);
        await _context.SaveChangesAsync();
        return evento.Id;
    }

    public async Task Delete(Evento evento)
    {
        _context.Eventos.Remove(evento);
        await _context.SaveChangesAsync();
    }
}
