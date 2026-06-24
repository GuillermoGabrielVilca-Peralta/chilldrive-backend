-- Fase C — Idempotencia de eventos offline.
-- Agrega la columna `cliente_evento_id` (UUID generado en el cliente) a `eventos`
-- y un índice ÚNICO PARCIAL: garantiza que un reenvío de la cola offline con el
-- mismo idLocal no cree un evento duplicado, sin obligar a que los eventos sin
-- cliente lo traigan (NULL no participa del único).
--
-- Proyecto db-first (sin migraciones EF): ejecutar este script contra la base
-- PostgreSQL ANTES de desplegar el backend de la Fase C. Es idempotente:
-- se puede correr varias veces sin error.
--
--   psql -h <host> -U <user> -d <db> -f db/2026-06-24_eventos_cliente_evento_id.sql

ALTER TABLE eventos
    ADD COLUMN IF NOT EXISTS cliente_evento_id varchar(64);

CREATE UNIQUE INDEX IF NOT EXISTS uq_eventos_cliente_evento_id
    ON eventos (cliente_evento_id)
    WHERE cliente_evento_id IS NOT NULL;
