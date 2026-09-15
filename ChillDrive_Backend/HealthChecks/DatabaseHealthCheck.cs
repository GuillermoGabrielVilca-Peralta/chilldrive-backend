using ChillDrive_Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ChillDrive_Backend.HealthChecks;

/// <summary>
/// Health check que verifica que la API puede conectarse a PostgreSQL.
/// Se expone en GET /health para monitoreo, balanceadores de carga y pipelines de despliegue.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public DatabaseHealthCheck(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken   cancellationToken = default)
    {
        try
        {
            // OpenConnectionAsync deja que el error REAL (auth, SSL, red) se propague al catch,
            // a diferencia de CanConnectAsync que lo oculta devolviendo false.
            await _context.Database.OpenConnectionAsync(cancellationToken);
            await _context.Database.CloseConnectionAsync();
            return HealthCheckResult.Healthy("Base de datos accesible.");
        }
        catch (Exception ex)
        {
            // Encadenar los mensajes internos: Npgsql suele poner el detalle (ej. SSL /
            // password authentication failed) en la InnerException.
            var detalle = ex.Message;
            for (var inner = ex.InnerException; inner is not null; inner = inner.InnerException)
                detalle += " | " + inner.Message;
            return HealthCheckResult.Unhealthy($"No se puede conectar a la base de datos: {detalle}", ex);
        }
    }
}
