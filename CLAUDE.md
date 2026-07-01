# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

ChillDrive is an ASP.NET Core (net10.0) Web API for an intelligent vehicular detection / driver-scoring system. Drivers register vehicles and cameras, take trips (`viajes`) that the AI scores 0–100, earn ChillDrive points, and redeem them for partner (`aliado`) rewards. Backed by PostgreSQL via EF Core (Npgsql). All domain naming and API messages are in Spanish.

## Commands

Run from the repo root (`D:\ChillDrive\Backend\ChillDrive_Backend`).

```powershell
dotnet build                                          # build
dotnet run --project ChillDrive_Backend               # run (Swagger UI served at /swagger in Development)
dotnet ef migrations add <Name> --project ChillDrive_Backend   # add EF migration
dotnet ef database update --project ChillDrive_Backend         # apply migrations
dotnet test                                           # run unit tests (or use the IDE test runner)
```

Tests live in `ChillDrive_Backend.Tests` (xUnit + Moq): service-level unit tests that mock the repositories and use `FakeUnitOfWork`, so they need no database. `ChillDrive_Backend\ChillDrive_Backend.http` holds sample requests. No linter is configured.

## Architecture

Strict three-layer flow, one set of files per domain entity:

**Controller → Service → Repository → `ApplicationDbContext`**

- **Controllers** (`Controllers/`) are thin: route HTTP, deserialize a `RequestDto`, call the service, map the result to an `IActionResult`. Routes are `api/[controller]`.
- **Services** (`Services/` interface + `Services/Implements/`) hold all business logic and validation only. Cross-entity validation lives here (e.g. `ViajeService.Iniciar` injects `IUsuarioRepository` and `IVehiculoRepository` to verify ownership before creating). Services must NOT contain entity↔DTO mapping code.
- **Mappings** (`Models/DTOs/Mappings/`) hold all entity↔DTO conversion as static extension methods (no AutoMapper): `entity.ToResponse()`, `dto.ToEntity()`, and `dto.ApplyTo(entity)` for updates. Services call these instead of mapping inline. When adding/changing a DTO field, update the corresponding mapper here — not the service.
- **Constants** (`Models/Constants/`) hold shared business rules referenced by more than one layer: `NivelPuntos.Calcular(puntosHistoricos)` (regla de niveles usada por el repo al hacer upsert) y `MovimientoTipos` (tipos de movimiento de puntos). Agregar aquí cualquier valor de dominio que se necesite en múltiples capas.
- **Repositories** (`Repositories/` interface + `Repositories/Implements/`) wrap EF Core data access only; no business rules.

### Conventions

- **Service result tuples**: mutating service methods return `(bool success, string mensaje[, int? id])` instead of throwing. Controllers translate `success`/`mensaje` into `Ok` / `BadRequest` / `NotFound` with `new { mensaje }` JSON bodies. Follow this pattern for new operations rather than exceptions.
- **DTOs**: every entity has request and response DTOs under `Models/DTOs/Requests` and `Models/DTOs/Responses`. Entities are never returned directly from controllers. Note the **response DTO C# namespace is `...DTOs.Response`** (singular) even though the folder is `Responses` — match the existing `using`.
- **Business endpoints** beyond CRUD use `PATCH` with an action suffix, e.g. `PATCH api/viaje/{id}/finalizar`, `.../cancelar`.
- **Registration**: every new repository and service must be registered as `Scoped` in `Program.cs` (the `Repositorios` and `Servicios` blocks). DI by interface throughout.

### Data model

- Models and `ApplicationDbContext` were **scaffolded from an existing PostgreSQL database** (db-first). `OnModelCreating` carries explicit snake_case table/column mappings, PG defaults (`CURRENT_TIMESTAMP`, `gen_random_bytes`), the `pgcrypto` extension, and two read-only DB views mapped as keyless entities (`VRankingConductore`, `VSuscripcionesActiva`), exposed read-only through their own layer set: `RankingController` (`GET api/ranking`, leaderboard) and `SuscripcionActivaController` (`GET api/suscripcionactiva`, admin). Treat the database schema as the source of truth; prefer regenerating/aligning over hand-editing mappings.
- Points system: `puntos_usuario` (per-user balance + level) is updated in real time by `EventoService` (each AI event applies `ImpactoEventos` points and writes the immutable `movimientos_puntos` ledger); `ViajeService.Finalizar` no longer credits points (avoids double-counting). `canjes` (redemptions) carry a pgcrypto-generated QR code.
- `EventoService.Registrar(dto, usuarioActualId)` enforces two guards (Fase C offline): **idempotency** — events carry a client `idLocal` stored as `eventos.cliente_evento_id` (partial UNIQUE index `uq_eventos_cliente_evento_id WHERE NOT NULL`); a resend with a known `idLocal` returns success without duplicating or re-applying points (a `23505` race on concurrent resends is caught and treated as the same idempotent hit). **Ownership** — when a `viajeId` is present, the trip must belong to the authenticated user (`viaje.UsuarioId == usuarioActualId`). The `cliente_evento_id` column was added db-first via `db/2026-06-24_eventos_cliente_evento_id.sql` (no EF migrations folder; run the SQL before deploying).
- `ViajeService.Iniciar` enforces **one active trip per user**: if the user already has an `En Curso` trip, a same-day one (Perú local date, UTC-5 via `OffsetPeruHoras`) is *resumed* (returns its id, no new row); trips from previous days are auto-closed as `Finalizado` (net points summed from their events) before a new one is created. This prevents orphan `En Curso` trips piling up when the app is closed without finalizing. One-time cleanup of pre-existing orphans: `db/2026-06-24_cerrar_viajes_en_curso_huerfanos.sql`. App side: `ApiService.getOrCreateViajeActual` no longer blindly reuses the cached `viajeActualId` — it stores `viajeActualFecha` and only resumes the cached trip if it's same-day (Perú), otherwise it asks the backend (which resumes/creates). Trip `PuntosGanados` is always stored as `Math.Max(0, net)` (in `Finalizar` and when auto-closing) because the `chk_viajes_puntos` constraint requires `puntos_ganados >= 0` (a negative net threw `23514`); infractions are already debited from the driver's balance per-event.
- `EventoService.Registrar` also applies two Fase C.2 guards using the client's **detection time** (`EventoRequestDto.FechaDeteccion`, ISO-8601 UTC, sent by the phone): it is validated (not future >5min / older than 60 days, forced to UTC) and stored as `Evento.FechaEvento` (fallback `UtcNow`) — this is the real event time, NOT server-arrival, so an offline queue drained hours later keeps correct route ordering. **Anti-flood**: a same-type event in the same trip within ±`VentanaAntiFloodSegundos` (3s) of another — measured by detection time via `IEventoRepository.ExisteEventoTipoEnVentana` — is rejected (the 3s window sits well under the detector's 6s per-type cooldown, so legit offline batches never false-positive). GPS-coherence checks were deliberately deferred (the client reuses last-known `_ultimaPos` between quick events, so identical coords are legitimate).
- **Auth (JWT + refresh token, both stateless).** `AuthService.Login` returns an **access token** (audience `ChillDriveCliente`, `Jwt:ExpirationHours` = 24h) plus a **refresh token** (audience `ChillDriveRefresh`, `Jwt:RefreshExpirationDays` = 30d, carries only the user id + `token_type=refresh` claim). `POST /api/auth/refresh` (`[AllowAnonymous]`) validates the refresh token against the **refresh audience**, reloads the user (rejects if inactive), and issues a fresh **rotated** pair. The audience split is the key security boundary: `Program.cs`'s access-token validation requires `ChillDriveCliente`, so a refresh token is auto-rejected on protected endpoints (defense-in-depth: also checks `token_type`). No DB table — consistent with the stateless design (no server-side revocation; client logout just drops its stored refresh token). Roles come from the `Admin:Correos` list in config, embedded as a claim at token time.

### Configuration

Connection string lives in `appsettings.Development.json` (gitignored) under `ConnectionStrings.DefaultConnection`. `Program.cs` reads it via `GetConnectionString("DefaultConnection")`. The `OnConfiguring` scaffold override was removed. Secrets go in `appsettings.Development.json`; in prod use environment variables.

---

## Flutter Frontend

App at `D:\ChillDrive\Frontend\chilldrive_app`. Feature-complete, tested on physical device.

**Key packages:** `dio` (HTTP), `flutter_secure_storage` (JWT + local route storage), `sqflite` (offline event outbox), `connectivity_plus` (re-sync on reconnect), `uuid` (per-event id), `ultralytics_yolo` (YOLO on-device), `geolocator` (GPS), `sensors_plus` (accelerometer telematics), `flutter_map` + `latlong2` (OSM route map), `flutter_foreground_task` (persistent notification), `wakelock_plus`, `flutter_tts` (voice alerts), `qr_flutter`.

**Config:** backend URL default in `lib/config/app_config.dart` → `AppConfig.apiBaseUrlPorDefecto`. `AppConfig.apiBaseUrl` is a **runtime-mutable** static: `main()` calls `AppConfig.cargarOverride()` before `runApp` to load a user-set URL from storage; the **Configuración → Conexión** screen lets the user change it (with "Guardar y probar" against `/health`) and persists it — no recompile needed when switching WiFi. `logout()` preserves the URL override (via `AppConfig.storageKey`) even though it `deleteAll`s the rest.

**Architecture:** all HTTP via `ApiService` (`lib/api_service.dart`); setState only (no global state manager). Screens under `lib/screens/`. `ApiService` is the data layer — it also wraps local `flutter_secure_storage` (token, refreshToken, usuario, viaje activo, recorrido GPS, backend URL), so screens never touch storage directly. A Dio `onError` interceptor **auto-renews** the session: on a `401` for a non-auth request it calls `/api/auth/refresh` once (coalesced so N concurrent 401s trigger a single refresh), stores the rotated pair, and retries the original request — so a day-expired access token never forces a re-login. Trip distance is computed client-side (`lib/utils/geo.dart`, haversine over the GPS waypoints) and sent in `finalizarViaje` (`distanciaKm`), shown in the historial + detalle screens.

**Offline-first events** (`lib/data/`): events are not posted directly. `ApiService.registrarEvento` persists to a local SQLite outbox (`outbox_db.dart`, `OutboxDb`) FIRST, then attempts to send — no signal = stays queued, no error thrown. `SyncService` (`sync_service.dart`, started/stopped in `MainScreen`) drains the queue on reconnect (`connectivity_plus`) with backoff (5→15→30→60s); the actual send loop is `ApiService.sincronizarPendientes` (`_intentarEnviar` → `enviado`/`sinRed`/`descartado`). Transient 4xx (`401/403/408/429`) are kept queued, not discarded, so an expired token doesn't drop events (and the auto-refresh interceptor resolves most `401`s before the send even fails). Each event carries a client `idLocal` (uuid) that the backend uses as an **idempotency key** (Fase C, live): `POST /api/evento` with a known `idLocal` returns `200` without duplicating the event or re-applying points; new ones return `201`. The phone never writes points — only events; the backend computes points, so balances are not client-alterable. Keep new outbox/sync logic in `lib/data/`, not in screens.

**Sensor telematics** (`lib/deteccion/detector_conduccion.dart`): all maneuver-detection logic (thresholds, sliding window, cooldowns, peak-sample filter) lives here, isolated from the UI. `deteccion_screen.dart` only wires the detector's callbacks → TTS + `POST /api/evento`. Keep new detection rules in this file, not in the screen. **Vision-detection classification rules** (YOLO class + speed → `TipoEvento`, pedestrian-risk scoring by bounding box) live in `lib/deteccion/clasificador_eventos.dart` (`ClasificadorEventos`, pure static methods, unit-tested in `test/clasificador_eventos_test.dart`) — also kept out of the screen. Braking/acceleration need a sustained accelerometer peak (≥150 ms) **plus** a GPS speed delta; cornering uses GPS heading only and is evaluated before the peak gate (a constant-speed turn has no longitudinal spike). New `TipoEvento` values must be added to BOTH `EventoService.TiposValidos` AND the `eventos.tipo_evento` CHECK constraint (else PG `23514`).

**Gotchas:**
- Foreground service class MUST be `com.pravera.flutter_foreground_task.service.ForegroundService` (v8.x — `ForegroundTaskService` doesn't exist).
- TFLite models go as **Flutter assets** (`pubspec.yaml` `assets:`), NOT in `android/app/src/main/assets`.
- `kotlin.incremental=false` in `android/gradle.properties` (project on D:, pub cache on C: → different roots crash incremental Kotlin).
- PiP mode: `MainActivity.kt` overrides `onUserLeaveHint` → `enterPictureInPictureMode`; controlled via MethodChannel `chilldrive/pip`; state flows back via EventChannel `chilldrive/pip_status`.
- `background_service.dart` holds the foreground task entry point (`@pragma('vm:entry-point')`) to break the circular import: main → main_screen → viaje_tab → deteccion_screen → main.
