# ESTADO DEL PROYECTO — ChillDrive (Backend + App)

> Documento de handoff. Si retomas en una sesión nueva, lee esto primero para tener todo el contexto.
> **Estado actual:** backend feature-complete + app Flutter completa con detección on-device, segundo plano (foreground service + PiP), alertas de voz inteligentes, **telemática por sensores (frenado/aceleración/giro brusco + uso de celular)**, **score por categoría de conducción**, **mapa de ruta con eventos y coloreado por agresividad**, y todas las pantallas operativas.
>
> **Control de versiones:** ambos repos están en Git. Rama de trabajo actual: `feature/sensores-telematica` (en GitHub: `GuillermoGabrielVilca-Peralta/chilldrive-app`). El trabajo de telemática vive ahí; pendiente merge a `main` tras validar en celular.

---

## 1. Qué es el proyecto

**ChillDrive** es una API Web en **ASP.NET Core (net10.0)** para un sistema de detección vehicular / scoring de conductores. Los conductores registran vehículos y cámaras, hacen **viajes**, la IA detecta señales de tránsito (eventos) que **suman o restan puntos** en tiempo real, ganan puntos ChillDrive, suben de nivel y los canjean por premios de aliados.

- **Backend:** ASP.NET Core net10.0, **PostgreSQL** vía **EF Core (Npgsql)**, **scaffolded db-first** (la BD es la fuente de verdad). En `D:\ChillDrive\Backend\ChillDrive_Backend`.
- **App (Flutter):** en `D:\ChillDrive\Frontend\chilldrive_app`. Completa y probada en celular físico.
- **Dominio en español** (nombres y mensajes).
- **IA (on-device, FUNCIONANDO):** modelo YOLOv8 entrenado (señales: semáforos + límites de velocidad) exportado a TFLite, corre en el celular vía `ultralytics_yolo`. Detecta → la app decide `TipoEvento` con el GPS → `POST /api/evento` → el backend puntúa. Segundo modelo (yolov8n COCO, personas) integrado para alertas de seguridad.

---

## 2. Cómo correrlo (IMPORTANTE)

- **NO hay .NET SDK en la terminal.** Todo se compila / ejecuta / testea desde **Rider** (IDE).
- Ejecutar con el perfil **http** o **https** de `launchSettings.json` → fija `ASPNETCORE_ENVIRONMENT=Development` (necesario para cargar secretos).
- **Swagger:** `http://localhost:5126/swagger` (contrato vivo y autogenerado).
- **Health check:** `http://localhost:5126/health` → `Healthy` si la BD responde.
- **Base de datos:** PostgreSQL local, db `chilldrive`, user `postgres`, password `admin`.
- **Tests:** proyecto `ChillDrive_Backend.Tests` (xUnit + Moq). Correr en Rider. ~35 tests.
- **Para probar con el celular físico:** correr el backend con el perfil **`lan`** en Rider (escucha en `0.0.0.0:5126`). La URL del backend está en **`lib/config/app_config.dart`** → `AppConfig.apiBaseUrl`. PC y celular en el mismo WiFi; firewall permitido.

### Cómo crear un usuario ADMIN
1. Pon el correo en `appsettings.Development.json` → `Admin:Correos`.
2. Registra un usuario con ese correo (`POST /api/usuario`).
3. Login → el token traerá el rol `Admin`.

### El asistente SÍ puede consultar la BD directamente
Conexión Python/psycopg2: `host=localhost dbname=chilldrive user=postgres password=admin`. Útil para ver CHECK constraints o validar inserts en transacción + rollback.

---

## 3. Arquitectura por capas — Backend (respetar SIEMPRE)

**Controller → Service → Repository → `ApplicationDbContext`**, un set de archivos por entidad.

- **Controllers** (`Controllers/`): delgados. Rutean, validan pertenencia/rol, traducen tuplas a HTTP. Heredan de **`ApiControllerBase`** (centraliza `[ApiController]`, ruta `api/[controller]` y expone `UsuarioActualId`).
- **Services** (`Services/` + `Services/Implements/`): TODA la lógica de negocio. NO mapean entidad↔DTO. Devuelven tuplas `(bool success, string mensaje[, int? id])`.
- **Mappings** (`Models/DTOs/Mappings/`): conversión entidad↔DTO como métodos de extensión (`ToResponse()`, `ToEntity(...)`, `ApplyTo(...)`). TODO el armado de DTOs vive aquí.
- **Constants** (`Models/Constants/`): valores de dominio — `NivelPuntos`, `MovimientoTipos`, `Roles`, `ImpactoEventos`.
- **Repositories** (`Repositories/` + `Implements/`): solo acceso a datos EF Core.
- **DI:** todo `Scoped` por interfaz en `Program.cs`. `PasswordService` es `Singleton`.
- **UnitOfWork** (`IUnitOfWork.ExecuteInTransactionAsync`): transacciones multi-tabla.

---

## 4. Arquitectura — App Flutter

Proyecto `D:\ChillDrive\Frontend\chilldrive_app\lib`.

```
lib/
  config/
    app_config.dart         ← URL del backend (cambiar aquí al pasar de entorno)
  background_service.dart   ← Entry point del foreground service (isolate Android)
  api_service.dart          ← Cliente HTTP (Dio) + endpoints + storage local (token, viaje, recorrido GPS) + outbox de eventos
  main.dart                 ← AuthGate: login ↔ main_screen
  data/
    outbox_db.dart          ← Bandeja local SQLite (sqflite) de eventos pendientes; offline-first (Fase A)
    sync_service.dart       ← Sincronizador: reenvía la cola al recuperar señal, con backoff (Fase B)
  deteccion/
    detector_conduccion.dart ← Detector de maniobras bruscas (acelerómetro + GPS); aislado de la UI
  screens/
    login_screen.dart
    registro_screen.dart
    main_screen.dart        ← Bottom nav: Dashboard / Viaje / Premios / Config
    dashboard_screen.dart   ← Stats, retos, racha, botón iniciar viaje
    viaje_tab.dart          ← Estado del viaje activo, botón abrir cámara
    deteccion_screen.dart   ← Detección YOLO + foreground service + PiP + TTS (930 líneas)
    premios_screen.dart     ← Catálogo de premios por aliado + canje con QR
    canje_screen.dart       ← Confirmación de canje + mostrar QR
    configuracion_screen.dart
    perfil_screen.dart
    historial_screen.dart   ← Historial de viajes
    detalle_viaje_screen.dart ← 3 tabs: General (cabecera + eventos), Calificación (score por categoría del viaje, en cliente) y Mapa (ruta + eventos)
    ranking_screen.dart     ← 2 tabs: Ranking (podio + leaderboard) y Mi conducción (score por categoría)
    racha_screen.dart
    nivel_screen.dart
```

**Convenciones Flutter:**
- Toda llamada HTTP pasa por `ApiService` (importar desde `../api_service.dart`). `ApiService` también centraliza el storage local (`flutter_secure_storage`): token, usuario, viaje activo y recorrido GPS.
- La URL base está en `lib/config/app_config.dart` → `AppConfig.apiBaseUrl`.
- No hay state management global (setState directo); aceptable para este tamaño de app.
- La lógica de sensores (umbrales, ventanas, cooldowns) vive en `lib/deteccion/detector_conduccion.dart`, NO en la pantalla. La pantalla solo cablea callbacks → TTS + `POST /api/evento`.
- `deteccion_screen.dart` es el archivo más complejo — tiene secciones marcadas con `// ──` para navegación.

---

## 5. Qué está implementado (TODO funcionando)

### Backend
- BCrypt + JWT, autorización global, pertenencia por usuario, roles Admin.
- Sistema de puntos: scoring en tiempo real, ledger inmutable `movimientos_puntos`.
- Transacciones `IUnitOfWork`, health check, rate limiting login, CORS.
- Vistas de BD: ranking, suscripciones activas.
- Tests: ~35 (NivelPuntos, Password, PuntosUsuario, Canje, Evento, Viaje, Suscripcion).

### App Flutter — detección y viaje
- **YOLO on-device** (TFLite): señales de tránsito (Green/Red Light, Speed Limit 30–90) y personas (yolov8n COCO). Toggle entre modelos.
- **Confirmación de 5 frames** antes de registrar un evento (evita falsos positivos).
- **GPS real** vía `geolocator`; velocidad simulada para demos sin moverse.
- **Foreground service** (`flutter_foreground_task`): notificación persistente mientras el viaje está activo. Muestra: `🟢 Viaje #N · X pts`, timer, estado validez, y lo que detecta YOLO en tiempo real (`Viendo: Red Light` / `Último: Señal Respetada`). Se actualiza cada 5 segundos.
- **Modo PiP** (Picture-in-Picture): al presionar Home durante un viaje, la app se minimiza a una ventana flotante. La detección YOLO sigue corriendo. Muestra timer + última detección. Tap → expande a pantalla completa.
- **Alertas de voz TTS** (`flutter_tts`, español) con cooldown de 10 s por tipo:
  - Red Light + vel > 5 km/h → *"Semáforo en rojo adelante, reduce la velocidad"*
  - Speed Limit N + vel > N+5 → *"Exceso de velocidad. Límite N km/h"*
  - Speed Limit N + vel ≤ N+5 → *"Zona de N km/h"* (informativo)
- **Riesgo peatonal inteligente** (modo personas): evalúa posición y tamaño del bounding box (`normalizedBox`) de cada persona detectada. Solo alerta si la persona está en la trayectoria del vehículo (centrada) y es grande (cercana). Personas en los bordes (veredas) no generan alerta. 3 niveles: precaución / persona en vía / zona muy transitada.
- **WakeLock** (`wakelock_plus`): pantalla siempre encendida durante el viaje.
- Mínimo 5 min para que el viaje sea válido (barra de progreso). Cooldown de 6 s entre eventos del mismo tipo.

### App Flutter — telemática por sensores (`sensors_plus` + GPS)
Detector aislado en `lib/deteccion/detector_conduccion.dart`. Usa la magnitud del acelerómetro lineal (`userAccelerometerEventStream`, 20 Hz) — independiente de cómo esté montado el celular — confirmada SIEMPRE con la velocidad/rumbo del GPS para descartar baches y vibraciones.
- **Frenado brusco / Aceleración brusca:** pico de acelerómetro ≥ 3.5 m/s² (~0.35 g) **sostenido ≥ 150 ms (3 muestras)** + caída/subida de velocidad GPS ≥ 7 km/h en ventana de 2 s. El requisito de pico sostenido filtra baches (que duran ~1 muestra).
- **Giro brusco:** cambio de rumbo GPS ≥ 25° a velocidad ≥ **45 km/h** (el umbral alto descarta esquinas normales de ciudad). NO depende del acelerómetro: se evalúa antes del filtro de pico longitudinal.
- **Uso de celular:** `WidgetsBindingObserver.didChangeAppLifecycleState` → si la app pasa a `paused` durante un viaje activo (y NO está en PiP), tras un debounce de 2 s registra el evento. Volver antes de 2 s lo cancela.
- GPS configurado con `AndroidSettings(intervalDuration: 500 ms)` para confirmar cambios de velocidad en frenadas/aceleraciones cortas.
- Velocidad usada por el detector = `_velActual` (getter): GPS real o velocidad simulada en modo demo.

### App Flutter — score por categoría y mapa de ruta
- **Score por categoría de conducción** (tab "Mi conducción" en `ranking_screen.dart`): consume `GET /api/evento/usuario/{id}/estadisticas`. Muestra score general + 5 categorías (Velocidad, Frenado, Aceleración, Giros, Celular), cada una `max(0, 100 + impactos_negativos)`. Color verde/naranja/rojo por umbral.
- **Mapa de ruta** (tab "Mapa" en `detalle_viaje_screen.dart`): `flutter_map` + tiles OSM (gratis, sin API key). Polilínea de la ruta **coloreada por agresividad** (azul/naranja/rojo según el delta de velocidad entre waypoints consecutivos), marcadores de inicio/fin, marcadores de eventos por tipo, chips de filtro y atajo "ver en mapa" desde cada evento.
- **Recorrido GPS:** se captura un waypoint `[lat, lon, speed_kmh]` cada 10 s durante el viaje y se guarda en `flutter_secure_storage` al finalizar (clave `recorrido_{viajeId}`). SIN cambios de BD — el recorrido es local al dispositivo. Formato retrocompatible: viajes viejos (solo lat,lon) cargan con speed 0.

### App Flutter — pantallas
- Login, registro, dashboard (stats + retos semanales + racha + nivel), historial de viajes, detalle de viaje (General + Mapa), ranking (Leaderboard + Mi conducción), racha de días, nivel/puntos, catálogo de premios, canje con QR, perfil, configuración.

---

## 6. Modelo de puntos (clave)

| Operación | Qué hace | Ledger (tipo) |
|---|---|---|
| Evento "Señal Respetada" (+10) | suma al saldo en tiempo real | `Ganado` (+) |
| Evento "Señal Ignorada" (−20) | resta del saldo | `Ajuste` (−) |
| Acreditar manual (admin) | suma | `Ganado` (+) |
| Canjear premio | resta | `Canjeado` (−) |
| Anular canje | devuelve | `Ajuste` (+) |
| Finalizar viaje | NO acredita; guarda el neto del viaje | — |

> ⚠️ Los valores de `tipo` están enforzados por CHECK `chk_movpuntos_coherencia`. DEBEN coincidir EXACTO.

Valores por evento en `Models/Constants/ImpactoEventos.cs`: Señal Respetada +10, Carril Mantenido/Distancia Segura +5, Señal Ignorada −20, Carril Invadido −15, Frenado Brusco −10, Aceleración Brusca −8, Giro Brusco −8, Uso Celular −15, Exceso Velocidad −25, Peatón/Otro 0.

> Los tipos de telemática (`Frenado Brusco`, `Aceleración Brusca`, `Giro Brusco`, `Uso Celular`) están en el array `TiposValidos` de `EventoService` Y en el CHECK constraint `eventos.tipo_evento`. Al agregar un tipo nuevo hay que tocar AMBOS o Postgres lanza `23514`.

---

## 7. Mapa de endpoints (resumen)

- **Auth:** `POST /api/auth/login` (público).
- **Usuario:** `POST` (registro, público), `GET` (admin), `GET/{id}` `PUT/{id}` `DELETE/{id}` (dueño).
- **Vehiculo / Camara:** CRUD + `PATCH/{id}/estado` (dueño), `GET` (admin).
- **Viaje:** `POST` (iniciar), `PATCH/{id}/finalizar`, `PATCH/{id}/cancelar`, `DELETE/{id}`, gets.
- **Evento:** `POST` (registrar + scoring), gets, `GET/usuario/{id}/estadisticas` (score por categoría, dueño), `DELETE/{id}` (admin).
- **Aliado / Premio:** lectura (autenticado), escritura (admin).
- **Canje:** `POST`, `PATCH/{id}/anular`, `PATCH/qr/{codigo}` (admin), gets.
- **PuntosUsuario / MovimientoPunto:** gets (dueño), `POST/acreditar` (admin).
- **Suscripcion:** CRUD (dueño), `GET` (admin).
- **Ranking:** `GET /api/ranking`. **SuscripcionActiva:** `GET /api/suscripcionactiva` (admin).
- **Infra:** `/health` (público), `/swagger` (dev).

---

## 8. Decisiones técnicas y "gotchas" (NO repetir errores)

1. **No hay SDK en terminal** → compilar/correr/testear en Rider.
2. **Npgsql + timestamps:** `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` en la 1ª línea de `Program.cs`. NO quitar.
3. **Secretos:** `appsettings.Development.json` (gitignored). Entorno debe ser `Development`.
4. **Swagger:** Swashbuckle 10.x usa Microsoft.OpenApi 2.x → `OpenApiSecuritySchemeReference` con sobrecarga `document =>`.
5. **Conexión BD:** `DefaultConnection` bajo `ConnectionStrings` en `appsettings.json`. `OnConfiguring` del scaffold fue eliminado.
6. **CHECK constraints (~35):** el scaffold NO los captura. Valores de enums en DTO/Service DEBEN coincidir EXACTO o Postgres lanza `23514`. Ver sección 6 para los críticos.
7. **MaxLength de DTOs** deben igualar las columnas de `OnModelCreating`.
8. **Tests:** Moq + `FakeUnitOfWork`. Sin BD real.
9. **Android (app):** `usesCleartextTraffic="true"`, permisos INTERNET/CAMERA/LOCATION/FOREGROUND_SERVICE/FOREGROUND_SERVICE_LOCATION/POST_NOTIFICATIONS/WAKE_LOCK en `AndroidManifest.xml`. Servicio foreground: `com.pravera.flutter_foreground_task.service.ForegroundService` (clase EXACTA — `ForegroundTaskService` no existe). PiP: `android:supportsPictureInPicture="true"` en `<activity>`.
10. **Modelo TFLite:** asset de Flutter (`assets/models/x.tflite` en `pubspec.yaml`). NO en `android/app/src/main/assets`.
11. **Build Flutter en Windows (discos distintos):** `kotlin.incremental=false` en `android/gradle.properties`. `minSdk = 24`. Modo desarrollador de Windows activado.
12. **IP del backend:** `lib/config/app_config.dart` → `AppConfig.apiBaseUrl`. Un solo lugar para cambiar.
13. **foreground_task callback:** `@pragma('vm:entry-point')` en `startCallback` de `lib/background_service.dart` evita tree-shaking. NO mover a `main.dart` (circular imports: main → main_screen → viaje_tab → deteccion_screen → main).
14. **PiP nativo:** `MainActivity.kt` sobreescribe `onUserLeaveHint` para entrar a PiP al presionar Home (solo si hay viaje activo, controlado por MethodChannel `chilldrive/pip`). El estado PiP llega a Flutter por EventChannel `chilldrive/pip_status`.

---

## 9. Próximos pasos sugeridos

**Ya HECHO esta sesión** (telemática tipo Airbag): frenado/aceleración/giro brusco por sensores, uso de celular, score por categoría en ranking, y mapa de ruta con eventos + coloreado por agresividad. Pendientes reales:

1. **Merge `feature/sensores-telematica` → `main`** tras validar los nuevos detectores en celular real y afinar umbrales en carretera (los valores actuales son estimados, no calibrados en campo).
2. **Telemetría offline-first (cola de eventos) — PLAN POR FASES.** Hoy los eventos requieren internet para llegar al backend; en ruta Lima→provincia se pierden. La detección + alertas TTS YA funcionan offline; falta persistir+sincronizar el scoring. **Principio de diseño:** el evento se guarda local PRIMERO (siempre), y el envío al backend es un paso aparte con reintentos. El celular NUNCA toca la BD directo — siempre pasa por `POST /api/evento` (misma autenticación, pertenencia, `TiposValidos`, CHECK y scoring de hoy). La cola es un patrón *outbox* (bandeja de salida); no cambia el nivel de confianza actual, solo difiere el envío.
   - **Fase A — Outbox local (solo app) ✅ HECHO:** `sqflite` + `uuid` agregados. Data layer aislado en `lib/data/outbox_db.dart` (clase `OutboxDb` + `EventoPendiente`): tabla SQLite `eventos_pendientes` (`id_local UUID PK, viaje_id, camara_id, tipo_evento, latitud, longitud, timestamp, estado`). `ApiService.registrarEvento` ahora inserta local PRIMERO y llama a `_enviarPendiente`: éxito → borra de la cola; sin señal o 5xx → queda pendiente y NO lanza error; 4xx → descarta y propaga. El POST ya manda `idLocal` (el backend lo ignora hasta la Fase C). `ApiService.eventosPendientes()` cuenta lo no sincronizado. La pantalla de detección NO cambió (`_refrescarPuntos` ya tragaba sus errores). `flutter analyze` limpio, APK debug compila.
   - **Fase B — Sincronizador con backoff (solo app) ✅ HECHO:** `connectivity_plus` agregado. `lib/data/sync_service.dart` (`SyncService`) orquesta el CUÁNDO; el envío vive en `ApiService.sincronizarPendientes` (recorre la cola FIFO, corta apenas detecta que no hay señal). Reintento creciente 5s→15s→30s→60s; se reinicia al recuperar conexión. Se dispara al abrir la app, al recuperar señal (`onConnectivityChanged`) y se puede forzar con `sincronizarAhora()`. Arranca/para en `MainScreen` (`initState`/`dispose`), activo toda la sesión. `_intentarEnviar` ahora devuelve `enviado/sinRed/descartado`. `flutter analyze` limpio, APK debug compila. **Con A+B el flujo offline ya es end-to-end:** detecta sin señal → guarda → reenvía solo al volver internet.
   - **Fase C — Idempotencia + pertenencia (backend + 1 columna BD) ✅ HECHO (núcleo):** `POST /api/evento` acepta el `idLocal` del cliente y, si ya existe un evento con ese id, devuelve **200 sin duplicar ni volver a tocar puntos** (`duplicado: true`); creación normal sigue devolviendo 201. Implementado en `EventoService.Registrar(dto, usuarioActualId)`: chequeo rápido por `IEventoRepository.GetByClienteEventoId`, y captura de la violación de único `23505` para cerrar la **carrera** entre dos reenvíos casi simultáneos (índice único parcial `uq_eventos_cliente_evento_id`). Modelo `Evento.ClienteEventoId` + mapeo EF (`cliente_evento_id varchar(64)`, único filtrado `WHERE NOT NULL`), DTO `EventoRequestDto.IdLocal`. **Pertenencia (hueco de seguridad cerrado):** el viaje debe ser del usuario autenticado (`viaje.UsuarioId == UsuarioActualId`), antes cualquier token podía inyectar eventos en el viaje de otro. Columna nueva vía SQL db-first: `db/2026-06-24_eventos_cliente_evento_id.sql` (idempotente, correr antes de desplegar). Tests nuevos: idempotencia (no duplica ni suma puntos), idLocal nuevo (registra), viaje de otro usuario (falla). Nota de seguridad: el celular NUNCA envía puntos, solo eventos; los puntos los calcula y escribe SOLO el backend desde `ImpactoEventos`, así que el saldo no es alterable desde la app.
   - **Fase C.2 — Hora de detección + anti-flood ✅ HECHO:** (a) el celular ahora manda `fechaDeteccion` (ISO-8601 UTC, la del momento real del hecho) en el POST; el backend la valida (ni futura >5 min ni más vieja de 60 días, forzada a UTC) y la guarda como `FechaEvento`, con fallback a `UtcNow`. Esto **arregla un bug latente**: antes los eventos offline quedaban con la hora de LLEGADA al server, descuadrando el orden de la ruta. (b) **Anti-flood**: se descarta un evento del mismo tipo en el mismo viaje dentro de ±3 s de otro, medido por hora de DETECCIÓN (no de llegada), vía `IEventoRepository.ExisteEventoTipoEnVentana`. La ventana de 3 s queda muy por debajo del cooldown del detector (6 s), así que no hay falsos positivos ni siquiera al vaciar una cola offline de golpe. Constante `EventoService.VentanaAntiFloodSegundos`. Tests nuevos: evento muy seguido → descartado; `fechaDeteccion` → se usa como fecha del evento.
   - **Fase C.3 — Coherencia GPS (DIFERIDA a propósito):** rechazar un "Frenado Brusco" sin movimiento GPS real. **No se implementó** porque el cliente reutiliza la última posición conocida (`_ultimaPos`) entre eventos rápidos, así que dos eventos con coordenadas idénticas son LEGÍTIMOS → una validación server-side de coherencia GPS daría falsos positivos. Además el evento no lleva velocidad instantánea. La coherencia GPS ya se hace en el cliente (el frenado/aceleración exige delta de velocidad GPS). Se retomaría solo si el payload incluyera velocidad por muestra.
3. **Límite de velocidad por calle (OSM, Peru):** en vez de buscar `maxspeed` (cobertura ~20% en Peru), leer el `highway` de la vía vía Overpass API y mapear al límite legal del reglamento peruano (DS 016-2009-MTC): motorway/trunk 120, primary 80, secondary 60, tertiary/residential 50/30, fallback urbano 50. Con internet = preciso; sin internet = umbral fijo (~80 km/h). Genera evento `Exceso Velocidad`.
4. **Tab "Calificación" en detalle de viaje (Fase D) ✅ HECHO:** `detalle_viaje_screen.dart` ahora tiene 3 pestañas (General / Calificación / Mapa). La de Calificación muestra un anillo con el score general del viaje (promedio) + barras por categoría (Velocidad, Frenado, Aceleración, Giros, Celular), con la misma fórmula del backend (`score = max(0, 100 + suma de impactos negativos de la categoría)`). Se calcula 100% en cliente desde los eventos ya cargados → funciona offline, sin llamadas extra. `flutter analyze` limpio.
5. **Un solo viaje activo por usuario + clamp de puntos ✅ HECHO (fix):** antes nada impedía tener varios viajes "En Curso" colgados (la app se cerraba sin finalizar, logout, etc.) → el historial mostraba muchos "En Curso". Ahora `ViajeService.Iniciar`: si ya hay un viaje abierto **del mismo día** (hora Perú, UTC-5 vía `OffsetPeruHoras`) lo **retoma** (devuelve su id, no crea otro); los de **días anteriores** se **cierran como Finalizado** (neto de puntos de sus eventos) antes de crear el nuevo. Lado app: `getOrCreateViajeActual` ya no reutiliza el id local a ciegas — guarda `viajeActualFecha` y solo continúa el viaje cacheado si es de hoy; si cambió el día, pide al backend (que retoma/crea). **Fix asociado:** `PuntosGanados` se guarda con `Math.Max(0, neto)` en `Finalizar` y al cerrar abandonados, porque el constraint `chk_viajes_puntos` exige `puntos_ganados >= 0` (un neto negativo lanzaba `23514`); las infracciones ya se descontaron del saldo evento por evento. Limpieza puntual de huérfanos: `db/2026-06-24_cerrar_viajes_en_curso_huerfanos.sql`. Tests: retoma el de hoy / cierra el de ayer y crea nuevo.
6. **Distancia real del viaje ✅ HECHO:** antes `finalizarViaje` mandaba `distanciaKm: 0` fijo y se perdía el dato aunque el viaje ya recolecta waypoints GPS. Ahora `lib/utils/geo.dart` calcula la distancia con **haversine** sobre el recorrido (`distanciaKmDeRecorrido`, helper puro y testeable, 6 tests) y se envía en `finalizarViaje`. Se muestra como métrica en `detalle_viaje_screen` y en cada tarjeta del `historial_screen`. Backend ya lo soportaba (`FinalizarViajeRequestDto.DistanciaKm [Range(0, 99999.99)]` → `viaje.DistanciaKm`). `flutter analyze` limpio.
7. **URL del backend configurable en runtime ✅ HECHO:** antes la IP era `const` en `app_config.dart` → cada cambio de WiFi obligaba a recompilar. Ahora `AppConfig.apiBaseUrl` es un static mutable que `main()` carga desde almacenamiento (`cargarOverride`) antes de `runApp`; **Configuración → Conexión → Servidor backend** permite editarla con "Guardar y probar" (GET `/health`) y persiste. `logout()` la preserva pese al `deleteAll`. `flutter analyze` limpio.
8. **Refresh token — sesión sin re-login diario ✅ HECHO:** el access token expiraba en 24h y solo existía `/login`. Ahora `AuthService.Login` devuelve access **+ refresh token** (JWT de 30 días, audience `ChillDriveRefresh` distinta a la de acceso → `Program.cs` lo rechaza en endpoints protegidos; marca `token_type=refresh`). `POST /api/auth/refresh` (`[AllowAnonymous]`) valida y emite un par **rotado**, recargando al usuario (corta si quedó inactivo). Stateless, sin tabla nueva. App: el interceptor `onError` de Dio **auto-renueva** en un 401 y reintenta la request (refresh coalescido para no disparar N a la vez). Tests nuevos: `AuthServiceTests` (6, auth no tenía ninguno). `flutter analyze` limpio; backend a compilar en Rider.
9. **Configurar el backend desde el login ✅ HECHO:** la IP solo se podía cambiar en Configuración (post-login), así que un celular que no conectaba quedaba trabado sin poder entrar. El diálogo de conexión se extrajo a `lib/widgets/dialogo_conexion.dart` (reutilizable) y se agregó un botón **"Configurar servidor"** en el login. Aplica el cambio sobre el `ApiService` recibido, así el mismo cliente que hace login apunta a la URL nueva. `configuracion_screen` usa el mismo diálogo (sin duplicar). `flutter analyze` limpio.
10. **Puntaje real al finalizar viaje:** hoy el cliente manda `puntajeConduccion: 100` fijo; calcularlo en backend desde los eventos del viaje.
11. **Despliegue:** Dockerfile + docker-compose (API + PostgreSQL) + variables de entorno.
12. **Backend (opcional):** tests de Camara/Aliado/Premio; paginación en endpoints de lista.

### Calibración de detección (iterativa, con feedback de manejo real)

Los umbrales están aislados **a propósito** para poder ajustarlos sin tocar UI ni backend. Es un proceso *data-driven*: se maneja, se anota qué detecta de más / de menos, y se ajusta el número. No es una tarea que "se termina" — se afina con el uso. Parámetros y dónde tocarlos:

**Maniobras bruscas — `lib/deteccion/detector_conduccion.dart`** (todo son `static const` al inicio de la clase):
- `_umbralAcel = 3.5` m/s² (~0.35 g) — pico de acelerómetro para frenado/aceleración. Subir si detecta baches como frenados; bajar si no capta frenadas reales.
- `_deltaVelMin = 7` km/h — cambio de velocidad GPS en la ventana para confirmar frenado/aceleración.
- `_deltaRumboMin = 25`° y `_velMinimaGiro = 45` km/h — **giro brusco** (el que falta pulir): un giro salta si el rumbo GPS cambia ≥25° a ≥45 km/h. Si salta en curvas normales de carretera → subir grados o velocidad; si no capta giros agresivos → bajarlos.
- `_velMinima = 12` km/h — piso para evaluar cualquier evento.
- `_ventana = 2s`, `_ventanaMinima = 800ms`, `_cooldown = 6s` (por tipo), `_muestreo = 50ms` (20 Hz), `_muestrasMinimas = 3` (150 ms de pico sostenido, filtra baches).

**Exceso de velocidad — `lib/deteccion/clasificador_eventos.dart`** (`senalAEvento`):
- Hoy dispara `Exceso Velocidad` con `kmh > limite` (sin tolerancia). El **aviso de voz** usa `limite + 5` (en `_alertaAudio` de `deteccion_screen.dart`). Si querés margen antes de penalizar, agregar una tolerancia aquí.

**Riesgo peatonal — `lib/deteccion/clasificador_eventos.dart`** (`mensajeRiesgoPeatonal`):
- Altura del bounding box: `0.28` (cercano), `0.48` (muy cercano). Centrado: `0.20–0.80` del ancho. Velocidad: `>35` km/h (frena ahora), `>20` (zona transitada). Scoring por puntos (1/2/4). Tests en `test/clasificador_eventos_test.dart` — **actualizar los tests al cambiar umbrales**.

**Puntos por evento — backend, `Models/Constants/ImpactoEventos.cs`** (cuánto suma/resta cada tipo).

---

## 10. Notas de memoria persistente

El asistente tiene memoria en `C:\Users\USER\.claude\projects\D--ChillDrive-Backend-ChillDrive-Backend\memory\`. Índice en `MEMORY.md`. Cubre: build sin SDK, validación dominio/BD, Swagger OpenApi 2.x, flujo de puntos/ledger, modelo de autorización, config/secretos, Npgsql timestamps, tests xUnit, app Flutter + cámara.
