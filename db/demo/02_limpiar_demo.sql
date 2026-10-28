-- Borra TODAS las cuentas ficticias de demostración (teléfonos 9000000xx, y la demo
-- vieja @demo.chilldrive.pe) con sus viajes, eventos y puntos. No toca cuentas reales.
BEGIN;

CREATE TEMP TABLE demo_ids ON COMMIT DROP AS
SELECT id FROM usuarios
WHERE telefono LIKE '9000000__' OR correo LIKE '%@demo.chilldrive.pe';

DELETE FROM movimientos_puntos WHERE usuario_id IN (SELECT id FROM demo_ids);
DELETE FROM eventos WHERE viaje_id IN (SELECT id FROM viajes WHERE usuario_id IN (SELECT id FROM demo_ids));
DELETE FROM viajes WHERE usuario_id IN (SELECT id FROM demo_ids);
DELETE FROM canjes WHERE usuario_id IN (SELECT id FROM demo_ids);
-- suscripciones, puntos_usuario, vehículos y cámaras se borran en cascada con el usuario.
DELETE FROM usuarios WHERE id IN (SELECT id FROM demo_ids);

COMMIT;
