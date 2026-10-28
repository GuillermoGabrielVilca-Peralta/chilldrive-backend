-- Borra TODAS las cuentas ficticias de demostración (correos @demo.chilldrive.pe)
-- y sus datos. No toca ninguna cuenta real.
BEGIN;

DELETE FROM movimientos_puntos
WHERE usuario_id IN (SELECT id FROM usuarios WHERE correo LIKE '%@demo.chilldrive.pe');

-- suscripciones, puntos_usuario y vehículos se borran en cascada con el usuario.
DELETE FROM usuarios WHERE correo LIKE '%@demo.chilldrive.pe';

COMMIT;
