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

    public async Task<Evento?> GetByClienteEventoId(string clienteEventoId)
    {
        return await _context.Eventos
            .FirstOrDefaultAsync(e => e.ClienteEventoId == clienteEventoId);
    }

    // ¿Ya hay un evento de este tipo en este viaje dentro de la ventana [desde, hasta]?
    // Se usa como defensa anti-flood: dos eventos del mismo tipo demasiado seguidos
    // (por hora de detección) son no-físicos y se descartan.
    public async Task<bool> ExisteEventoTipoEnVentana(int viajeId, string tipoEvento, DateTime desde, DateTime hasta)
    {
        return await _context.Eventos.AnyAsync(e =>
            e.ViajeId == viajeId &&
            e.TipoEvento == tipoEvento &&
            e.FechaEvento >= desde &&
            e.FechaEvento <= hasta);
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

    public async Task<List<(string Tipo, int TotalImpacto, int Cantidad)>> GetImpactoNegativoPorTipoParaUsuario(int usuarioId)
    {
        var resultados = await _context.Eventos
            .Where(e => e.ViajeId.HasValue && e.Viaje!.UsuarioId == usuarioId && e.ImpactoPuntaje < 0)
            .GroupBy(e => e.TipoEvento)
            .Select(g => new
            {
                Tipo         = g.Key,
                TotalImpacto = g.Sum(e => e.ImpactoPuntaje),
                Cantidad     = g.Count()
            })
            .ToListAsync();

        return resultados.Select(r => (r.Tipo, r.TotalImpacto, r.Cantidad)).ToList();
    }
}
