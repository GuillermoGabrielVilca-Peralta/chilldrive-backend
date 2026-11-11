-- Borra TODAS las cuentas ficticias de demostración con sus viajes, eventos y puntos.
-- Se reconocen por la contraseña demo compartida (Demo1234!, mismo hash exacto), los
-- teléfonos 9000000xx de antes y la demo vieja @demo.chilldrive.pe. No toca cuentas reales.
BEGIN;

CREATE TEMP TABLE demo_ids ON COMMIT DROP AS
SELECT id FROM usuarios
WHERE password_hash = '$2a$12$vYIauyQmZVRn7hZA9qKMte159Z2a/67htDmAf0.YDKsb/Tus16vMS'
   OR telefono LIKE '9000000__' OR correo LIKE '%@demo.chilldrive.pe';

DELETE FROM movimientos_puntos WHERE usuario_id IN (SELECT id FROM demo_ids);
DELETE FROM eventos WHERE viaje_id IN (SELECT id FROM viajes WHERE usuario_id IN (SELECT id FROM demo_ids));
DELETE FROM viajes WHERE usuario_id IN (SELECT id FROM demo_ids);
DELETE FROM canjes WHERE usuario_id IN (SELECT id FROM demo_ids);
-- suscripciones, puntos_usuario, vehículos y cámaras se borran en cascada con el usuario.
DELETE FROM usuarios WHERE id IN (SELECT id FROM demo_ids);

COMMIT;
