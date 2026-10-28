-- Suscripciones con pago verificado (Yape / efectivo), prueba gratis y pago con puntos.
--
-- Antes: el conductor creaba su propia suscripción (incluso por S/ 0) y quedaba
-- "Profesional" al instante, sin ningún pago. Ahora las suscripciones pagadas con
-- dinero nacen "Pendiente" y solo un administrador las aprueba desde el panel.
--
-- Proyecto db-first (sin migraciones EF): ejecutar este script contra la base
-- PostgreSQL (Neon) ANTES de desplegar el backend que usa estas columnas.
-- Es idempotente: se puede correr varias veces sin error.
--
--   psql -h <host> -U <user> -d <db> -f db/2026-10-27_suscripciones_pagos.sql

-- ── Columnas nuevas ─────────────────────────────────────────────────────────
ALTER TABLE suscripciones
    ADD COLUMN IF NOT EXISTS plan             varchar(20) NOT NULL DEFAULT 'Mensual',
    ADD COLUMN IF NOT EXISTS dias             integer     NOT NULL DEFAULT 30,
    ADD COLUMN IF NOT EXISTS codigo_pago      varchar(12),
    ADD COLUMN IF NOT EXISTS puntos_usados    integer,
    ADD COLUMN IF NOT EXISTS fecha_aprobacion timestamp,
    ADD COLUMN IF NOT EXISTS motivo_rechazo   varchar(255);

COMMENT ON COLUMN suscripciones.plan IS 'Prueba | Mensual | Trimestral | Anual | Puntos';
COMMENT ON COLUMN suscripciones.dias IS 'Días de plan Profesional que otorga (incluye los meses de regalo)';
COMMENT ON COLUMN suscripciones.codigo_pago IS 'Código que el conductor presenta al pagar en efectivo';
COMMENT ON COLUMN suscripciones.referencia_pago IS 'Número de operación de Yape informado por el conductor';

-- ── Restricciones actualizadas ──────────────────────────────────────────────
-- La prueba gratis y el pago con puntos cuestan S/ 0.
ALTER TABLE suscripciones DROP CONSTRAINT IF EXISTS chk_suscripciones_monto;
ALTER TABLE suscripciones ADD  CONSTRAINT chk_suscripciones_monto CHECK (monto_soles >= 0);

ALTER TABLE suscripciones DROP CONSTRAINT IF EXISTS chk_suscripciones_pago;
ALTER TABLE suscripciones ADD  CONSTRAINT chk_suscripciones_pago CHECK (
    metodo_pago IN ('Yape', 'Plin', 'Tarjeta', 'Efectivo', 'Puntos', 'Gratis', 'Otro'));

ALTER TABLE suscripciones DROP CONSTRAINT IF EXISTS chk_suscripciones_estado;
ALTER TABLE suscripciones ADD  CONSTRAINT chk_suscripciones_estado CHECK (
    estado IN ('Pendiente', 'Activa', 'Rechazada', 'Vencida', 'Cancelada'));

ALTER TABLE suscripciones DROP CONSTRAINT IF EXISTS chk_suscripciones_plan;
ALTER TABLE suscripciones ADD  CONSTRAINT chk_suscripciones_plan CHECK (
    plan IN ('Prueba', 'Mensual', 'Trimestral', 'Anual', 'Puntos'));

ALTER TABLE suscripciones DROP CONSTRAINT IF EXISTS chk_suscripciones_dias;
ALTER TABLE suscripciones ADD  CONSTRAINT chk_suscripciones_dias CHECK (dias > 0);

-- ── Reglas anti-abuso garantizadas por la base ──────────────────────────────
-- Un solo pago en verificación por conductor.
CREATE UNIQUE INDEX IF NOT EXISTS uq_suscripciones_pendiente_usuario
    ON suscripciones (usuario_id) WHERE estado = 'Pendiente';

-- Una sola prueba gratis por cuenta (aunque luego venza o se cancele).
CREATE UNIQUE INDEX IF NOT EXISTS uq_suscripciones_prueba_usuario
    ON suscripciones (usuario_id) WHERE plan = 'Prueba';

-- Un mismo número de operación de Yape no puede usarse para dos pagos.
CREATE UNIQUE INDEX IF NOT EXISTS uq_suscripciones_referencia_yape
    ON suscripciones (referencia_pago) WHERE metodo_pago = 'Yape' AND referencia_pago IS NOT NULL;

-- Códigos de pago en efectivo únicos.
CREATE UNIQUE INDEX IF NOT EXISTS uq_suscripciones_codigo_pago
    ON suscripciones (codigo_pago) WHERE codigo_pago IS NOT NULL;
