# ESTADO DEL PROYECTO — ChillDrive (Backend + App)

> Documento de handoff. Si retomas en una sesión nueva, lee esto primero para tener todo el contexto.
> **Estado actual:** backend feature-complete + app Flutter completa con detección on-device, segundo plano (foreground service + PiP), alertas de voz inteligentes, y todas las pantallas operativas.

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
  api_service.dart          ← Cliente HTTP (Dio) + todos los endpoints; usa AppConfig
  main.dart                 ← AuthGate: login ↔ main_screen
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
    detalle_viaje_screen.dart
    ranking_screen.dart     ← Leaderboard con podio
    racha_screen.dart
    nivel_screen.dart
```

**Convenciones Flutter:**
- Toda llamada HTTP pasa por `ApiService` (importar desde `../api_service.dart`).
- La URL base está en `lib/config/app_config.dart` → `AppConfig.apiBaseUrl`.
- No hay state management global (setState directo); aceptable para este tamaño de app.
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

### App Flutter — pantallas
- Login, registro, dashboard (stats + retos semanales + racha + nivel), historial de viajes, detalle de viaje, ranking con podio, racha de días, nivel/puntos, catálogo de premios, canje con QR, perfil, configuración.

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

Valores por señal en `Models/Constants/ImpactoEventos.cs`: Señal Respetada +10, Carril Mantenido/Distancia Segura +5, Señal Ignorada −20, Carril Invadido −15, Frenado Brusco −10, Exceso Velocidad −25, Peatón/Otro 0.

---

## 7. Mapa de endpoints (resumen)

- **Auth:** `POST /api/auth/login` (público).
- **Usuario:** `POST` (registro, público), `GET` (admin), `GET/{id}` `PUT/{id}` `DELETE/{id}` (dueño).
- **Vehiculo / Camara:** CRUD + `PATCH/{id}/estado` (dueño), `GET` (admin).
- **Viaje:** `POST` (iniciar), `PATCH/{id}/finalizar`, `PATCH/{id}/cancelar`, `DELETE/{id}`, gets.
- **Evento:** `POST` (registrar + scoring), gets, `DELETE/{id}` (admin).
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

1. **Frenado / aceleración brusca:** detector con `sensors_plus` (acelerómetro lineal) + validación con velocidad GPS. Umbral ~3.5 m/s². Nuevos TipoEvento en el backend: `"Frenado Brusco"` (ya tiene ImpactoEventos asignado), `"Aceleración Brusca"`.
2. **Señales desde mapa (OpenStreetMap):** al iniciar un viaje, precarga los límites de velocidad y nodos con señales del recorrido vía Overpass API (gratuita). Complementa la cámara para vías sin señal visible.
3. **Despliegue:** Dockerfile + docker-compose (API + PostgreSQL) + variables de entorno.
4. **Backend (opcional):** tests de Camara/Aliado/Premio; paginación en endpoints de lista.

---

## 10. Notas de memoria persistente

El asistente tiene memoria en `C:\Users\USER\.claude\projects\D--ChillDrive-Backend-ChillDrive-Backend\memory\`. Índice en `MEMORY.md`. Cubre: build sin SDK, validación dominio/BD, Swagger OpenApi 2.x, flujo de puntos/ledger, modelo de autorización, config/secretos, Npgsql timestamps, tests xUnit, app Flutter + cámara.
