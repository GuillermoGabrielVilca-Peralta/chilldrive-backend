namespace ChillDrive_Backend.Models.Constants;

/// <summary>
/// Catálogo de planes del plan Profesional y reglas de la suscripción. Es la única fuente
/// de precios y duraciones: el cliente solo elige el código del plan, nunca envía montos.
/// Los valores de <c>Plan</c>, <c>Estado</c> y <c>MetodoPago</c> deben coincidir con los
/// CHECK de la tabla <c>suscripciones</c> (ver db/2026-10-27_suscripciones_pagos.sql).
/// </summary>
public static class PlanesSuscripcion
{
    // ── Códigos de plan ─────────────────────────────────────────────────────
    public const string Prueba     = "Prueba";
    public const string Mensual    = "Mensual";
    public const string Trimestral = "Trimestral";
    public const string Anual      = "Anual";
    public const string Puntos     = "Puntos";

    // ── Estados de una suscripción ──────────────────────────────────────────
    public const string Pendiente = "Pendiente";   // pago informado, esperando verificación del admin
    public const string Activa    = "Activa";
    public const string Rechazada = "Rechazada";
    public const string Vencida   = "Vencida";
    public const string Cancelada = "Cancelada";

    // ── Métodos de pago ─────────────────────────────────────────────────────
    public const string Yape          = "Yape";
    public const string Efectivo      = "Efectivo";
    public const string MetodoPuntos  = "Puntos";
    public const string MetodoGratis  = "Gratis";

    /// <summary>Métodos con los que el conductor puede pagar un plan con dinero.</summary>
    public static readonly string[] MetodosConDinero = [Yape, Efectivo];

    // ── Plan del usuario (columna usuarios.tipo_plan) ───────────────────────
    public const string TipoGratuito    = "Gratuito";
    public const string TipoProfesional = "Profesional";

    /// <summary>Puntos ChillDrive que cuesta un mes de plan Profesional.</summary>
    public const int PuntosPorMes = 1500;

    /// <summary>A partir de cuántos días restantes la app avisa que el plan está por vencer.</summary>
    public const int DiasAvisoVencimiento = 5;

    public record Plan(
        string  Codigo,
        string  Nombre,
        decimal PrecioSoles,
        int     Dias,
        string  Descripcion,
        string? Etiqueta,
        bool    SePagaConDinero);

    public static readonly IReadOnlyList<Plan> Todos =
    [
        new(Prueba,     "Prueba gratis",  0m,   30,  "30 días del plan Profesional sin pagar. Una sola vez por cuenta.", "Gratis",          false),
        new(Mensual,    "Mensual",        15m,  30,  "Plan Profesional por 30 días.",                                    null,              true),
        new(Trimestral, "Trimestral",     45m,  120, "Paga 3 meses y recibe 4.",                                         "1 mes gratis",    true),
        new(Anual,      "Anual",          150m, 365, "Paga 10 meses y recibe 12.",                                       "2 meses gratis",  true),
        new(Puntos,     "Con tus puntos", 0m,   30,  $"Canjea {PuntosPorMes} puntos ChillDrive por 30 días.",            "Sin pagar dinero", false),
    ];

    public static Plan? Buscar(string? codigo) =>
        Todos.FirstOrDefault(p => string.Equals(p.Codigo, codigo, StringComparison.OrdinalIgnoreCase));
}
