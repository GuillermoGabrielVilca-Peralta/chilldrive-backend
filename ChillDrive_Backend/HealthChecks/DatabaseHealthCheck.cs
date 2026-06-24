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
            // CanConnectAsync hace un ping ligero a la BD sin ejecutar consultas de negocio.
            return await _context.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Base de datos accesible.")
                : HealthCheckResult.Unhealthy("No se puede conectar a la base de datos.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Error al verificar la base de datos.", ex);
        }
    }
}
