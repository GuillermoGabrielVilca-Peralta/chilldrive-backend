using System.Text;
using System.Threading.RateLimiting;
using ChillDrive_Backend.HealthChecks;
using ChillDrive_Backend.Middleware;
using ChillDrive_Backend.Models;
using ChillDrive_Backend.Repositories;
using ChillDrive_Backend.Repositories.Implements;
using ChillDrive_Backend.Services;
using ChillDrive_Backend.Services.Implements;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

// Las columnas de fecha de la BD son 'timestamp without time zone'. Npgsql (v6+) rechaza
// escribir un DateTime con Kind=Utc (p.ej. DateTime.UtcNow) en ese tipo. Este switch activa
// el comportamiento legacy, que acepta esos valores. Debe ir ANTES de cualquier uso de Npgsql.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Los secretos de DESARROLLO (cadena de conexión con contraseña y Jwt:SecretKey) viven en
// appsettings.Development.json, que el framework carga automáticamente en entorno Development
// y que está excluido del repositorio (.gitignore). En producción se usan variables de entorno.
// Diagnóstico al arrancar: muestra el entorno y si los secretos cargaron (sin imprimir valores).
Console.WriteLine($"[Config] Entorno         : {builder.Environment.EnvironmentName}");
Console.WriteLine($"[Config] ConnectionString: {(string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")) ? "VACÍA" : "OK")}");
Console.WriteLine($"[Config] Jwt:SecretKey   : {(string.IsNullOrWhiteSpace(builder.Configuration["Jwt:SecretKey"]) ? "VACÍA" : "OK")}");

// Nombre de la política CORS — se usa al registrar y al activar en el pipeline
const string CorsPolicyName = "ChillDrivePolicy";

// Nombre de la política de rate limiting aplicada al login (anti fuerza bruta)
const string LoginRateLimitPolicy = "login";

// ─── DbContext ────────────────────────────────────────────────────────────────
// La cadena de conexión viene de configuración (appsettings → User Secrets → variables de entorno).
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión 'ConnectionStrings:DefaultConnection'. " +
        "Configúrala en User Secrets (dev) o en la variable de entorno ConnectionStrings__DefaultConnection (prod).");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// ─── Repositorios ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUsuarioRepository,         UsuarioRepository>();
builder.Services.AddScoped<IVehiculoRepository,        VehiculoRepository>();
builder.Services.AddScoped<ICamaraRepository,          CamaraRepository>();
builder.Services.AddScoped<IViajeRepository,           ViajeRepository>();
builder.Services.AddScoped<IEventoRepository,          EventoRepository>();
builder.Services.AddScoped<IAliadoRepository,          AliadoRepository>();
builder.Services.AddScoped<IPremioRepository,          PremioRepository>();
builder.Services.AddScoped<ICanjeRepository,           CanjeRepository>();
builder.Services.AddScoped<IPuntosUsuarioRepository,   PuntosUsuarioRepository>();
builder.Services.AddScoped<ISuscripcionRepository,     SuscripcionRepository>();
builder.Services.AddScoped<IMovimientoPuntoRepository, MovimientoPuntoRepository>();

// Repositorios de solo lectura sobre vistas de la BD.
builder.Services.AddScoped<IRankingRepository,            RankingRepository>();
builder.Services.AddScoped<ISuscripcionActivaRepository, SuscripcionActivaRepository>();

// Unidad de trabajo: transacciones para operaciones que tocan varias tablas.
builder.Services.AddScoped<IUnitOfWork,                UnitOfWork>();

// ─── Servicios de infraestructura ────────────────────────────────────────────
// PasswordService es Singleton porque no tiene estado; la misma instancia sirve
// para todos los requests sin riesgo de concurrencia.
builder.Services.AddSingleton<IPasswordService, PasswordService>();

// ─── Servicios ────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService,           AuthService>();
builder.Services.AddScoped<IUsuarioService,        UsuarioService>();
builder.Services.AddScoped<IVehiculoService,       VehiculoService>();
builder.Services.AddScoped<ICamaraService,         CamaraService>();
builder.Services.AddScoped<IViajeService,          ViajeService>();
builder.Services.AddScoped<IEventoService,         EventoService>();
builder.Services.AddScoped<IAliadoService,         AliadoService>();
builder.Services.AddScoped<IPremioService,         PremioService>();
builder.Services.AddScoped<ICanjeService,          CanjeService>();
builder.Services.AddScoped<IPuntosUsuarioService,  PuntosUsuarioService>();
builder.Services.AddScoped<ISuscripcionService,    SuscripcionService>();
builder.Services.AddScoped<IMovimientoPuntoService, MovimientoPuntoService>();
builder.Services.AddScoped<IRankingService,            RankingService>();
builder.Services.AddScoped<ISuscripcionActivaService, SuscripcionActivaService>();

// ─── Autenticación JWT ────────────────────────────────────────────────────────
// Lee la configuración desde la sección "Jwt" (appsettings + User Secrets / variables de entorno).
var jwtSettings    = builder.Configuration.GetSection("Jwt");
var secretKeyValue = jwtSettings["SecretKey"];
if (string.IsNullOrWhiteSpace(secretKeyValue))
    throw new InvalidOperationException(
        "Falta 'Jwt:SecretKey'. Configúrala en User Secrets (dev) o en la variable de entorno Jwt__SecretKey (prod).");

var secretKey = Encoding.UTF8.GetBytes(secretKeyValue);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Valida que el token fue emitido por esta API
            ValidateIssuer           = true,
            ValidIssuer              = jwtSettings["Issuer"],

            // Valida que el token es para este sistema (ChillDriveCliente)
            ValidateAudience         = true,
            ValidAudience            = jwtSettings["Audience"],

            // Verifica que el token no haya expirado
            ValidateLifetime         = true,

            // Verifica que la firma del token sea válida con nuestra clave secreta
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(secretKey),

            // Sin margen de tiempo para la expiración — el token expira exactamente cuando dice
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ─── Rate limiting ────────────────────────────────────────────────────────────
// Limita los intentos de login por IP para mitigar ataques de fuerza bruta:
// máximo 5 intentos por minuto; el 6.º recibe 429 Too Many Requests.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(LoginRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window      = TimeSpan.FromMinutes(1)
            }));
});

// ─── Health checks ────────────────────────────────────────────────────────────
// Expone GET /health verificando la conexión a la base de datos.
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

// ─── CORS ─────────────────────────────────────────────────────────────────────
// Los orígenes se leen desde "Cors:AllowedOrigins" en appsettings.json.
// En Development, appsettings.Development.json sobreescribe con los localhost de frontend.
// En Production, la lista queda vacía (bloquea todo) hasta que se configure el dominio real.
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        policy
            .WithOrigins(origins)          // solo los dominios registrados en config
            .AllowAnyHeader()              // necesario para Authorization: Bearer y Content-Type
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE");
                                           // OPTIONS es manejado automáticamente por el middleware
    });
});

// ─── Controllers + Swagger ───────────────────────────────────────────────────
// Política global: TODOS los endpoints requieren autenticación por defecto.
// Para declarar un endpoint público se usa [AllowAnonymous] de forma explícita.
// Esto garantiza que un controller nuevo quede protegido sin que nadie tenga que recordarlo.
builder.Services.AddControllers(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
})
.ConfigureApiBehaviorOptions(options =>
{
    // Reemplaza el ValidationProblemDetails por defecto con el formato { mensaje }
    // que usa el resto de la API, para que los errores de validación sean consistentes.
    options.InvalidModelStateResponseFactory = context =>
    {
        var errores = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(err => err.ErrorMessage))
            .ToList();

        var mensaje = string.Join(" | ", errores);
        return new BadRequestObjectResult(new { mensaje });
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "ChillDrive API",
        Version     = "v1",
        Description = "API del sistema inteligente de detección vehicular ChillDrive"
    });

    // Definición del esquema de seguridad JWT para Swagger UI
    // Permite enviar el token desde el botón "Authorize" de Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name        = "Authorization",
        Type        = SecuritySchemeType.Http,
        Scheme      = "Bearer",
        BearerFormat = "JWT",
        In          = ParameterLocation.Header,
        Description = "Ingresa el token JWT obtenido en /api/auth/login. Ejemplo: Bearer eyJhbG..."
    });

    // Aplica el esquema Bearer a todos los endpoints que lo requieran.
    // En Microsoft.OpenApi 2.x (Swashbuckle 10.x) la referencia al esquema se hace con
    // OpenApiSecuritySchemeReference y la sobrecarga que recibe el documento OpenAPI;
    // ya no existe la antigua OpenApiReference / ReferenceType.
    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

var app = builder.Build();

// ─── Pipeline ─────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ChillDrive API v1");
        // Swagger UI queda en /swagger (no en la raíz) para una URL clara.
        options.RoutePrefix = "swagger";
    });
}

// Debe ser el primero en el pipeline para capturar excepciones de todo lo que viene después.
// En desarrollo devuelve el mensaje real de la excepción; en producción, uno genérico.
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

// CORS debe ir antes de Authentication/Authorization para que el preflight OPTIONS
// reciba los headers correctos sin que el JWT middleware lo rechace primero.
app.UseCors(CorsPolicyName);

// Rate limiter: evalúa las políticas (p.ej. la del login) antes de llegar al controller.
app.UseRateLimiter();

// Orden obligatorio: Authentication antes que Authorization
// Authentication = "¿quién eres?" | Authorization = "¿qué puedes hacer?"
app.UseAuthentication();
app.UseAuthorization();

// GET /health — endpoint público (no pasa por el filtro de autorización de MVC) para monitoreo.
app.MapHealthChecks("/health");

app.MapControllers();
app.Run();
