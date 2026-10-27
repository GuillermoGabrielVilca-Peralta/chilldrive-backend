-- Monitoreo en vivo — última ubicación conocida de cada viaje.
-- Durante un viaje "En Curso" la app envía su posición cada ~10 s
-- (PATCH api/viaje/{id}/ubicacion) y el panel de administración la lee
-- (GET api/monitoreo/viajes-activos) para dibujar los vehículos en el mapa.
-- Solo se guarda la ÚLTIMA posición (no el historial): basta para el mapa en vivo
-- y evita que la tabla crezca con cada envío.
--
-- Proyecto db-first (sin migraciones EF): ejecutar este script contra la base
-- PostgreSQL (Neon) ANTES de desplegar el backend que usa estas columnas; si no,
-- toda consulta sobre `viajes` falla por columnas inexistentes. Es idempotente:
-- se puede correr varias veces sin error.
--
--   psql -h <host> -U <user> -d <db> -f db/2026-10-27_viajes_ultima_ubicacion.sql

ALTER TABLE viajes
    ADD COLUMN IF NOT EXISTS ultima_latitud         numeric(10, 7),
    ADD COLUMN IF NOT EXISTS ultima_longitud        numeric(10, 7),
    ADD COLUMN IF NOT EXISTS ultima_velocidad_kmh   numeric(6, 2),
    ADD COLUMN IF NOT EXISTS ultima_ubicacion_fecha timestamp;

COMMENT ON COLUMN viajes.ultima_ubicacion_fecha IS
    'Momento (UTC) de la última posición recibida; el panel la usa para saber si el vehículo sigue transmitiendo';

-- Acelera la consulta del panel: viajes en curso con posición reciente.
CREATE INDEX IF NOT EXISTS idx_viajes_ultima_ubicacion
    ON viajes (ultima_ubicacion_fecha DESC)
    WHERE estado = 'En Curso';
