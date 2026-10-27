using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ChillDrive_Backend.Models;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Aliado> Aliados { get; set; }

    public virtual DbSet<Camara> Camaras { get; set; }

    public virtual DbSet<Canje> Canjes { get; set; }

    public virtual DbSet<Evento> Eventos { get; set; }

    public virtual DbSet<MovimientosPunto> MovimientosPuntos { get; set; }

    public virtual DbSet<Premio> Premios { get; set; }

    public virtual DbSet<PuntosUsuario> PuntosUsuarios { get; set; }

    public virtual DbSet<Suscripcione> Suscripciones { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<VRankingConductore> VRankingConductores { get; set; }

    public virtual DbSet<VSuscripcionesActiva> VSuscripcionesActivas { get; set; }

    public virtual DbSet<Vehiculo> Vehiculos { get; set; }

    public virtual DbSet<Viaje> Viajes { get; set; }

    // La cadena de conexión se configura por inyección de dependencias en Program.cs
    // (AddDbContext + UseNpgsql), leyéndola de la configuración (appsettings / User Secrets /
    // variables de entorno). Se eliminó el OnConfiguring hardcodeado del scaffold para no
    // exponer credenciales en el código ni pisar la configuración de DI.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<Aliado>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_aliados");

            entity.ToTable("aliados");

            entity.HasIndex(e => e.Ruc, "uq_aliados_ruc").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Categoria)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Otro'::character varying")
                .HasColumnName("categoria");
            entity.Property(e => e.ComisionPorcentaje)
                .HasPrecision(5, 2)
                .HasDefaultValue(10.00m)
                .HasComment("% que ChillDrive cobra al aliado por cada canje redimido")
                .HasColumnName("comision_porcentaje");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.Direccion)
                .HasMaxLength(200)
                .HasColumnName("direccion");
            entity.Property(e => e.Distrito)
                .HasMaxLength(60)
                .HasColumnName("distrito");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Activo'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.Logo)
                .HasMaxLength(255)
                .HasColumnName("logo");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.Ruc)
                .HasMaxLength(11)
                .HasColumnName("ruc");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .HasColumnName("telefono");
        });

        modelBuilder.Entity<Camara>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_camaras");

            entity.ToTable("camaras", tb => tb.HasComment("Dispositivos de captura de video por vehículo"));

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DireccionIp)
                .HasMaxLength(50)
                .HasColumnName("direccion_ip");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Desconectada'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.Posicion)
                .HasMaxLength(30)
                .HasDefaultValueSql("'Frontal'::character varying")
                .HasColumnName("posicion");
            entity.Property(e => e.VehiculoId).HasColumnName("vehiculo_id");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.Camaras)
                .HasForeignKey(d => d.VehiculoId)
                .HasConstraintName("fk_camaras_vehiculo");
        });

        modelBuilder.Entity<Canje>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_canjes");

            entity.ToTable("canjes");

            entity.HasIndex(e => e.Estado, "idx_canjes_estado");

            entity.HasIndex(e => e.PremioId, "idx_canjes_premio");

            entity.HasIndex(e => e.UsuarioId, "idx_canjes_usuario");

            entity.HasIndex(e => e.CodigoQr, "uq_canjes_codigo_qr").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CodigoQr)
                .HasMaxLength(64)
                .HasDefaultValueSql("encode(gen_random_bytes(16), 'hex'::text)")
                .HasComment("Hash hexadecimal de 32 chars generado con pgcrypto, único por canje")
                .HasColumnName("codigo_qr");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Pendiente'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.FechaCanje)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_canje");
            entity.Property(e => e.FechaUso)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_uso");
            entity.Property(e => e.PremioId).HasColumnName("premio_id");
            entity.Property(e => e.PuntosUtilizados).HasColumnName("puntos_utilizados");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Premio).WithMany(p => p.Canjes)
                .HasForeignKey(d => d.PremioId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_canjes_premio");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Canjes)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_canjes_usuario");
        });

        modelBuilder.Entity<Evento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_eventos");

            entity.ToTable("eventos");

            entity.HasIndex(e => e.CamaraId, "idx_eventos_camara");

            entity.HasIndex(e => e.FechaEvento, "idx_eventos_fecha").IsDescending();

            entity.HasIndex(e => e.ViajeId, "idx_eventos_viaje");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CamaraId).HasColumnName("camara_id");
            entity.Property(e => e.FechaEvento)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_evento");
            entity.Property(e => e.FotoEvidencia)
                .HasMaxLength(255)
                .HasColumnName("foto_evidencia");
            entity.Property(e => e.ImpactoPuntaje)
                .HasComment("Positivo = suma al puntaje del viaje; negativo = resta")
                .HasColumnName("impacto_puntaje");
            entity.Property(e => e.Latitud)
                .HasPrecision(10, 7)
                .HasColumnName("latitud");
            entity.Property(e => e.Longitud)
                .HasPrecision(10, 7)
                .HasColumnName("longitud");
            entity.Property(e => e.TipoEvento)
                .HasMaxLength(60)
                .HasColumnName("tipo_evento");
            entity.Property(e => e.ClienteEventoId)
                .HasMaxLength(64)
                .HasColumnName("cliente_evento_id");
            // Único solo cuando hay idLocal (índice parcial): garantiza idempotencia
            // sin obligar a que los eventos sin cliente lo traigan.
            entity.HasIndex(e => e.ClienteEventoId, "uq_eventos_cliente_evento_id")
                .IsUnique()
                .HasFilter("cliente_evento_id IS NOT NULL");
            entity.Property(e => e.ViajeId)
                .HasComment("NULL si la detección ocurre fuera de un viaje activo")
                .HasColumnName("viaje_id");

            entity.HasOne(d => d.Camara).WithMany(p => p.Eventos)
                .HasForeignKey(d => d.CamaraId)
                .HasConstraintName("fk_eventos_camara");

            entity.HasOne(d => d.Viaje).WithMany(p => p.Eventos)
                .HasForeignKey(d => d.ViajeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_eventos_viaje");
        });

        modelBuilder.Entity<MovimientosPunto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_movimientos_puntos");

            entity.ToTable("movimientos_puntos", tb => tb.HasComment("Libro mayor inmutable: cada fila es una transacción de puntos"));

            entity.HasIndex(e => e.CanjeId, "idx_movpuntos_canje");

            entity.HasIndex(e => e.FechaMovimiento, "idx_movpuntos_fecha").IsDescending();

            entity.HasIndex(e => e.UsuarioId, "idx_movpuntos_usuario");

            entity.HasIndex(e => e.ViajeId, "idx_movpuntos_viaje");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CanjeId)
                .HasComment("FK a canjes; se agrega con ALTER TABLE después de crear esa tabla")
                .HasColumnName("canje_id");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .HasColumnName("descripcion");
            entity.Property(e => e.FechaMovimiento)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_movimiento");
            entity.Property(e => e.Puntos).HasColumnName("puntos");
            entity.Property(e => e.Tipo)
                .HasMaxLength(20)
                .HasColumnName("tipo");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.ViajeId).HasColumnName("viaje_id");

            entity.HasOne(d => d.Canje).WithMany(p => p.MovimientosPuntos)
                .HasForeignKey(d => d.CanjeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_movpuntos_canje");

            entity.HasOne(d => d.Usuario).WithMany(p => p.MovimientosPuntos)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_movpuntos_usuario");

            entity.HasOne(d => d.Viaje).WithMany(p => p.MovimientosPuntos)
                .HasForeignKey(d => d.ViajeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_movpuntos_viaje");
        });

        modelBuilder.Entity<Premio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_premios");

            entity.ToTable("premios", tb => tb.HasComment("Catálogo de premios canjeables con puntos ChillDrive"));

            entity.HasIndex(e => e.AliadoId, "idx_premios_aliado");

            entity.HasIndex(e => e.Estado, "idx_premios_estado");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AliadoId).HasColumnName("aliado_id");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Disponible'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
            entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.Imagen)
                .HasMaxLength(255)
                .HasColumnName("imagen");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .HasColumnName("nombre");
            entity.Property(e => e.PuntosRequeridos).HasColumnName("puntos_requeridos");
            entity.Property(e => e.Stock).HasColumnName("stock");

            entity.HasOne(d => d.Aliado).WithMany(p => p.Premios)
                .HasForeignKey(d => d.AliadoId)
                .HasConstraintName("fk_premios_aliado");
        });

        modelBuilder.Entity<PuntosUsuario>(entity =>
        {
            entity.HasKey(e => e.UsuarioId).HasName("pk_puntos_usuario");

            entity.ToTable("puntos_usuario");

            entity.Property(e => e.UsuarioId)
                .ValueGeneratedNever()
                .HasColumnName("usuario_id");
            entity.Property(e => e.Nivel)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Bronce'::character varying")
                .HasColumnName("nivel");
            entity.Property(e => e.PuntosActuales).HasColumnName("puntos_actuales");
            entity.Property(e => e.PuntosHistoricos)
                .HasComment("Acumulado total histórico; no baja con canjes. Determina nivel y ranking.")
                .HasColumnName("puntos_historicos");
            entity.Property(e => e.UltimaActualizacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ultima_actualizacion");

            entity.HasOne(d => d.Usuario).WithOne(p => p.PuntosUsuario)
                .HasForeignKey<PuntosUsuario>(d => d.UsuarioId)
                .HasConstraintName("fk_puntos_usuario_usuario");
        });

        modelBuilder.Entity<Suscripcione>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_suscripciones");

            entity.ToTable("suscripciones", tb => tb.HasComment("Ciclos de pago del plan Profesional (15 soles/mes)"));

            entity.HasIndex(e => e.Estado, "idx_suscripciones_estado");

            entity.HasIndex(e => e.FechaFin, "idx_suscripciones_fin");

            entity.HasIndex(e => e.UsuarioId, "idx_suscripciones_usuario");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Activa'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
            entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.MetodoPago)
                .HasMaxLength(30)
                .HasDefaultValueSql("'Yape'::character varying")
                .HasColumnName("metodo_pago");
            entity.Property(e => e.MontoSoles)
                .HasPrecision(8, 2)
                .HasDefaultValue(15.00m)
                .HasColumnName("monto_soles");
            entity.Property(e => e.ReferenciaPago)
                .HasMaxLength(100)
                .HasColumnName("referencia_pago");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Suscripciones)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("fk_suscripciones_usuario");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_usuarios");

            entity.ToTable("usuarios", tb => tb.HasComment("Conductores registrados en ChillDrive"));

            entity.HasIndex(e => e.Correo, "idx_usuarios_correo").IsUnique();

            entity.HasIndex(e => e.Estado, "idx_usuarios_estado");

            entity.HasIndex(e => e.Correo, "uq_usuarios_correo").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Apellidos)
                .HasMaxLength(100)
                .HasColumnName("apellidos");
            entity.Property(e => e.Correo)
                .HasMaxLength(150)
                .HasColumnName("correo");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Activo'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.FotoPerfil)
                .HasMaxLength(255)
                .HasColumnName("foto_perfil");
            entity.Property(e => e.Nombres)
                .HasMaxLength(100)
                .HasColumnName("nombres");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .HasColumnName("telefono");
            entity.Property(e => e.TipoConductor)
                .HasMaxLength(30)
                .HasDefaultValueSql("'Particular'::character varying")
                .HasComment("Segmenta para plan Profesional: taxis, delivery, mototaxis")
                .HasColumnName("tipo_conductor");
            entity.Property(e => e.TipoPlan)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Gratuito'::character varying")
                .HasComment("Gratuito = app básica | Profesional = 15 soles/mes")
                .HasColumnName("tipo_plan");
            entity.Property(e => e.UltimoAcceso)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ultimo_acceso");
        });

        modelBuilder.Entity<VRankingConductore>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_ranking_conductores");

            entity.Property(e => e.Conductor).HasColumnName("conductor");
            entity.Property(e => e.Nivel)
                .HasMaxLength(20)
                .HasColumnName("nivel");
            entity.Property(e => e.PuntajePromedio).HasColumnName("puntaje_promedio");
            entity.Property(e => e.PuntosActuales).HasColumnName("puntos_actuales");
            entity.Property(e => e.PuntosHistoricos).HasColumnName("puntos_historicos");
            entity.Property(e => e.TipoConductor)
                .HasMaxLength(30)
                .HasColumnName("tipo_conductor");
            entity.Property(e => e.TotalViajes).HasColumnName("total_viajes");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
        });

        modelBuilder.Entity<VSuscripcionesActiva>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_suscripciones_activas");

            entity.Property(e => e.Conductor).HasColumnName("conductor");
            entity.Property(e => e.Correo)
                .HasMaxLength(150)
                .HasColumnName("correo");
            entity.Property(e => e.DiasRestantes).HasColumnName("dias_restantes");
            entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
            entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
            entity.Property(e => e.MetodoPago)
                .HasMaxLength(30)
                .HasColumnName("metodo_pago");
            entity.Property(e => e.MontoSoles)
                .HasPrecision(8, 2)
                .HasColumnName("monto_soles");
            entity.Property(e => e.SuscripcionId).HasColumnName("suscripcion_id");
            entity.Property(e => e.TipoConductor)
                .HasMaxLength(30)
                .HasColumnName("tipo_conductor");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
        });

        modelBuilder.Entity<Vehiculo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_vehiculos");

            entity.ToTable("vehiculos", tb => tb.HasComment("Vehículos registrados por cada conductor"));

            entity.HasIndex(e => e.UsuarioId, "idx_vehiculos_usuario");

            entity.HasIndex(e => e.Placa, "uq_vehiculos_placa").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Anio).HasColumnName("anio");
            entity.Property(e => e.Color)
                .HasMaxLength(30)
                .HasColumnName("color");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Activo'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.Marca)
                .HasMaxLength(50)
                .HasColumnName("marca");
            entity.Property(e => e.Modelo)
                .HasMaxLength(50)
                .HasColumnName("modelo");
            entity.Property(e => e.Placa)
                .HasMaxLength(15)
                .HasColumnName("placa");
            entity.Property(e => e.TipoVehiculo)
                .HasMaxLength(30)
                .HasDefaultValueSql("'Auto'::character varying")
                .HasColumnName("tipo_vehiculo");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Vehiculos)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("fk_vehiculos_usuario");
        });

        modelBuilder.Entity<Viaje>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_viajes");

            entity.ToTable("viajes");

            entity.HasIndex(e => e.Estado, "idx_viajes_estado");

            entity.HasIndex(e => e.FechaInicio, "idx_viajes_fecha_inicio").IsDescending();

            entity.HasIndex(e => e.UsuarioId, "idx_viajes_usuario");

            entity.HasIndex(e => e.VehiculoId, "idx_viajes_vehiculo");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DistanciaKm)
                .HasPrecision(10, 2)
                .HasColumnName("distancia_km");
            entity.Property(e => e.DuracionMinutos).HasColumnName("duracion_minutos");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'En Curso'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.FechaFin)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_fin");
            entity.Property(e => e.FechaInicio)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_inicio");
            entity.Property(e => e.PuntajeConduccion)
                .HasPrecision(5, 2)
                .HasComment("Score 0-100 generado por modelo de IA en tiempo real")
                .HasColumnName("puntaje_conduccion");
            entity.Property(e => e.PuntosGanados)
                .HasComment("Puntos ChillDrive acreditados al finalizar el viaje")
                .HasColumnName("puntos_ganados");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.VehiculoId).HasColumnName("vehiculo_id");
            entity.Property(e => e.UltimaLatitud)
                .HasPrecision(10, 7)
                .HasColumnName("ultima_latitud");
            entity.Property(e => e.UltimaLongitud)
                .HasPrecision(10, 7)
                .HasColumnName("ultima_longitud");
            entity.Property(e => e.UltimaVelocidadKmh)
                .HasPrecision(6, 2)
                .HasColumnName("ultima_velocidad_kmh");
            entity.Property(e => e.UltimaUbicacionFecha)
                .HasColumnType("timestamp without time zone")
                .HasComment("Momento (UTC) de la última posición recibida; el panel la usa para saber si el vehículo sigue transmitiendo")
                .HasColumnName("ultima_ubicacion_fecha");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Viajes)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_viajes_usuario");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.Viajes)
                .HasForeignKey(d => d.VehiculoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_viajes_vehiculo");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
