-- Tres conductores demo nuevos (sin viajes aún) que yapearon el plan Mensual (S/ 15)
-- y esperan verificación → aparecen en "Pagos por verificar".
-- Contraseña: Demo1234! (igual que la flota demo). Se puede correr más de una vez.
-- Teléfonos 900000016–018: 02_limpiar_demo.sql también los borra.

BEGIN;

CREATE TEMP TABLE nuevos ON COMMIT DROP AS
SELECT * FROM (VALUES
    ('Jorge Steven', 'Nina Leandres', 'jorgenina.l@gmail.com',         '900000016', 'Taxi',       interval '3 days', 'N5P-347', 'Toyota',    'Corolla', 2017, 'Blanco', '41617390', interval '35 minutes'),
    ('Mauricio J.',  'Rodríguez H.',  'mauriciorodriguez.h@gmail.com', '900000017', 'Particular', interval '2 days', 'R8D-192', 'Nissan',    'Versa',   2021, 'Azul',   '41623058', interval '15 minutes'),
    ('Esteban',      'Blas Aguilar',  'estebanblas.a@gmail.com',       '900000018', 'Taxi',       interval '1 day',  'S2G-675', 'Chevrolet', 'Sail',    2019, 'Rojo',   '41630412', interval '4 minutes')
) AS n(nombres, apellidos, correo, telefono, tipo_conductor, registrado_hace, placa, marca, modelo, anio, color,
       operacion, pago_hace);

DELETE FROM usuarios WHERE telefono IN (SELECT telefono FROM nuevos);   -- vehículo, cámara y suscripción en cascada

INSERT INTO usuarios (nombres, apellidos, correo, password_hash, telefono, tipo_plan, tipo_conductor, estado, fecha_registro, ultimo_acceso)
SELECT nombres, apellidos, correo, '$2a$12$vYIauyQmZVRn7hZA9qKMte159Z2a/67htDmAf0.YDKsb/Tus16vMS',
       telefono, 'Gratuito', tipo_conductor, 'Activo', now() - registrado_hace, now() - pago_hace - interval '2 minutes'
FROM nuevos;

INSERT INTO vehiculos (usuario_id, placa, marca, modelo, anio, color, tipo_vehiculo, estado, fecha_registro)
SELECT u.id, n.placa, n.marca, n.modelo, n.anio, n.color, 'Auto', 'Activo', u.fecha_registro + interval '15 minutes'
FROM nuevos n JOIN usuarios u ON u.telefono = n.telefono;

INSERT INTO camaras (vehiculo_id, nombre, posicion, estado, fecha_registro)
SELECT v.id, 'Cámara del celular', 'Frontal', 'Conectada', v.fecha_registro + interval '5 minutes'
FROM vehiculos v JOIN usuarios u ON u.id = v.usuario_id JOIN nuevos n ON n.telefono = u.telefono;

INSERT INTO suscripciones (usuario_id, plan, dias, fecha_inicio, fecha_fin, monto_soles, metodo_pago,
                           estado, referencia_pago, fecha_registro)
SELECT u.id, 'Mensual', 30, CURRENT_DATE, CURRENT_DATE + 30, 15.00, 'Yape',
       'Pendiente', n.operacion, now() - n.pago_hace
FROM nuevos n JOIN usuarios u ON u.telefono = n.telefono;

COMMIT;

SELECT u.nombres || ' ' || u.apellidos AS conductor, u.correo, v.placa,
       s.plan, s.metodo_pago, s.monto_soles, s.estado, s.referencia_pago
FROM usuarios u
JOIN vehiculos v ON v.usuario_id = u.id
JOIN suscripciones s ON s.usuario_id = u.id
WHERE u.telefono IN ('900000016', '900000017', '900000018')
ORDER BY u.telefono;
