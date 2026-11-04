-- Conductor demo nuevo: Wilder Salas Mayhua. Se acaba de registrar (sin viajes aún),
-- yapeó el plan Mensual y espera verificación → aparece en "Pagos por verificar".
-- Contraseña: Demo1234! (igual que la flota demo). Se puede correr más de una vez.
-- Teléfono 900000014: 02_limpiar_demo.sql también lo borra.

BEGIN;

DELETE FROM usuarios WHERE telefono = '900000014';   -- vehículo, cámara y suscripción en cascada

INSERT INTO usuarios (nombres, apellidos, correo, password_hash, telefono, tipo_plan, tipo_conductor, estado, fecha_registro, ultimo_acceso)
VALUES ('Wilder', 'Salas Mayhua', 'wildersalas.m@gmail.com',
        '$2a$12$vYIauyQmZVRn7hZA9qKMte159Z2a/67htDmAf0.YDKsb/Tus16vMS',
        '900000014', 'Gratuito', 'Taxi', 'Activo', now() - interval '1 day', now() - interval '25 minutes');

INSERT INTO vehiculos (usuario_id, placa, marca, modelo, anio, color, tipo_vehiculo, estado, fecha_registro)
SELECT id, 'K3W-518', 'Kia', 'Rio', 2020, 'Gris', 'Auto', 'Activo', fecha_registro + interval '15 minutes'
FROM usuarios WHERE telefono = '900000014';

INSERT INTO camaras (vehiculo_id, nombre, posicion, estado, fecha_registro)
SELECT v.id, 'Cámara del celular', 'Frontal', 'Conectada', v.fecha_registro + interval '5 minutes'
FROM vehiculos v JOIN usuarios u ON u.id = v.usuario_id
WHERE u.telefono = '900000014';

INSERT INTO suscripciones (usuario_id, plan, dias, fecha_inicio, fecha_fin, monto_soles, metodo_pago,
                           estado, referencia_pago, fecha_registro)
SELECT id, 'Mensual', 30, CURRENT_DATE, CURRENT_DATE + 30, 15.00, 'Yape',
       'Pendiente', '41588203', now() - interval '20 minutes'
FROM usuarios WHERE telefono = '900000014';

COMMIT;

SELECT u.nombres || ' ' || u.apellidos AS conductor, u.correo, v.placa,
       s.plan, s.metodo_pago, s.monto_soles, s.estado, s.referencia_pago
FROM usuarios u
JOIN vehiculos v ON v.usuario_id = u.id
JOIN suscripciones s ON s.usuario_id = u.id
WHERE u.telefono = '900000014';
