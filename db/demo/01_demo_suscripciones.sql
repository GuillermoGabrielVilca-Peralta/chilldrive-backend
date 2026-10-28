-- ChillDrive — usuarios FICTICIOS para ver el flujo de suscripciones de punta a punta.
--
-- Crea 9 conductores, cada uno en un estado distinto del ciclo de pago, para revisar
-- la app ("Mi plan" y el aviso del inicio) y el panel ("Suscripciones").
--
--   Contraseña de TODAS las cuentas demo:  Demo1234!
--   Correos:  <nombre>@demo.chilldrive.pe   (fáciles de identificar y de borrar)
--
-- Las fechas son relativas al día en que se ejecuta, así el escenario siempre es coherente.
-- Requiere haber corrido antes db/2026-10-27_suscripciones_pagos.sql.
-- Idempotente: borra las cuentas demo anteriores antes de crearlas de nuevo.
-- Para quitarlas: db/demo/02_limpiar_demo.sql

BEGIN;

-- ── Limpieza de una corrida anterior ────────────────────────────────────────
DELETE FROM movimientos_puntos
WHERE usuario_id IN (SELECT id FROM usuarios WHERE correo LIKE '%@demo.chilldrive.pe');
-- suscripciones, puntos_usuario y vehículos se borran en cascada con el usuario.
DELETE FROM usuarios WHERE correo LIKE '%@demo.chilldrive.pe';

-- ── Conductores ─────────────────────────────────────────────────────────────
-- Hash BCrypt (factor 12, igual que PasswordService) de "Demo1234!".
INSERT INTO usuarios (nombres, apellidos, correo, password_hash, telefono, tipo_plan, tipo_conductor, estado)
VALUES
    ('Carla',  'Quispe',  'carla.quispe@demo.chilldrive.pe',  '$2a$12$vYIauyQmZVRn7hZA9qKMte159Z2a/67htDmAf0.YDKsb/Tus16vMS', '900000001', 'Gratuito',    'Particular', 'Activo'),
    ('Diego',  'Ramos',   'diego.ramos@demo.chilldrive.pe',   '$2a$12$vYIauyQmZVRn7hZA9qKMte159Z2a/67htDmAf0.YDKsb/Tus16vMS', '900000002', 'Profesional', 'Taxi',       'Activo'),
    ('Lucía',  'Torres',  'lucia.torres@demo.chilldrive.pe',  '$2a$12$vYIauyQmZVRn7hZA9qKMte159Z2a/67htDmAf0.YDKsb/Tus16vMS', '900000003', 'Gratuito',    'Delivery',   'Activo'),
    ('Marco',  'Huamán',  'marco.huaman@demo.chilldrive.pe',  '$2a$12$vYIauyQmZVRn7hZA9qKMte159Z2a/67htDmAf0.YDKsb/Tus16vMS', '900000004', 'Gratuito',    'Mototaxi',   'Activo'),
    ('Sofía',  'Mendoza', 'sofia.mendoza@demo.chilldrive.pe', '$2a$12$vYIauyQmZVRn7hZA9qKMte159Z2a/67htDmAf0.YDKsb/Tus16vMS', '900000005', 'Profesional', 'Taxi',       'Activo'),
    ('Jorge',  'Paredes', 'jorge.paredes@demo.chilldrive.pe', '$2a$12$vYIauyQmZVRn7hZA9qKMte159Z2a/67htDmAf0.YDKsb/Tus16vMS', '900000006', 'Profesional', 'Delivery',   'Activo'),
    ('Rosa',   'Chávez',  'rosa.chavez@demo.chilldrive.pe',   '$2a$12$vYIauyQmZVRn7hZA9qKMte159Z2a/67htDmAf0.YDKsb/Tus16vMS', '900000007', 'Gratuito',    'Particular', 'Activo'),
    ('Pedro',  'Salas',   'pedro.salas@demo.chilldrive.pe',   '$2a$12$vYIauyQmZVRn7hZA9qKMte159Z2a/67htDmAf0.YDKsb/Tus16vMS', '900000008', 'Gratuito',    'Taxi',       'Activo'),
    ('Ana',    'Vargas',  'ana.vargas@demo.chilldrive.pe',    '$2a$12$vYIauyQmZVRn7hZA9qKMte159Z2a/67htDmAf0.YDKsb/Tus16vMS', '900000009', 'Profesional', 'Particular', 'Activo');

-- ── Puntos (Carla tiene suficientes para pagar un mes con puntos) ───────────
INSERT INTO puntos_usuario (usuario_id, puntos_actuales, puntos_historicos, nivel)
SELECT u.id, p.actuales, p.historicos, p.nivel
FROM (VALUES
    ('carla.quispe@demo.chilldrive.pe',  1800, 2600, 'Oro'),
    ('diego.ramos@demo.chilldrive.pe',    600,  900, 'Plata'),
    ('lucia.torres@demo.chilldrive.pe',   250,  400, 'Bronce'),
    ('marco.huaman@demo.chilldrive.pe',   430,  700, 'Plata'),
    ('sofia.mendoza@demo.chilldrive.pe', 2200, 5100, 'Platino'),
    ('jorge.paredes@demo.chilldrive.pe',  900, 1800, 'Oro'),
    ('rosa.chavez@demo.chilldrive.pe',    150, 1200, 'Plata'),
    ('pedro.salas@demo.chilldrive.pe',     80,  300, 'Bronce'),
    ('ana.vargas@demo.chilldrive.pe',     320, 2400, 'Oro')
) AS p(correo, actuales, historicos, nivel)
JOIN usuarios u ON u.correo = p.correo;

-- ── Suscripciones: un escenario por conductor ───────────────────────────────
-- Periodos [fecha_inicio, fecha_fin): vigente mientras hoy < fecha_fin.
INSERT INTO suscripciones (usuario_id, plan, dias, fecha_inicio, fecha_fin, monto_soles, metodo_pago,
                           estado, referencia_pago, codigo_pago, puntos_usados, fecha_aprobacion,
                           motivo_rechazo, fecha_registro)
SELECT u.id, s.plan, s.dias, CURRENT_DATE + s.ini, CURRENT_DATE + s.fin, s.monto, s.metodo,
       s.estado, s.ref, s.codigo, s.puntos,
       CASE WHEN s.aprobado_hace IS NULL THEN NULL ELSE now() - s.aprobado_hace END,
       s.motivo, now() - s.registrado_hace
FROM (VALUES
    -- Diego: está usando su prueba gratis (le quedan 20 días).
    ('diego.ramos@demo.chilldrive.pe',   'Prueba',     30, -10,  20,   0.00, 'Gratis',   'Activa',    NULL,       NULL,        NULL, interval '10 days', NULL, interval '10 days'),
    -- Lucía: yapeó el plan Mensual y espera verificación → aparece en "Pagos por verificar".
    ('lucia.torres@demo.chilldrive.pe',  'Mensual',    30,   0,  30,  15.00, 'Yape',     'Pendiente', '40817352', NULL,        NULL, NULL,               NULL, interval '2 hours'),
    -- Marco: pagará el Trimestral en efectivo con su código.
    ('marco.huaman@demo.chilldrive.pe',  'Trimestral', 120,  0, 120,  45.00, 'Efectivo', 'Pendiente', NULL,       'CD-7K3QXP', NULL, NULL,               NULL, interval '1 day'),
    -- Sofía: plan Anual aprobado hace un mes.
    ('sofia.mendoza@demo.chilldrive.pe', 'Anual',      365, -30, 335, 150.00, 'Yape',    'Activa',    '39120455', NULL,        NULL, interval '30 days', NULL, interval '30 days'),
    -- Jorge: plan Mensual que vence en 3 días → aviso "por vencer" y recordatorio.
    ('jorge.paredes@demo.chilldrive.pe', 'Mensual',    30, -27,   3,  15.00, 'Yape',     'Activa',    '38800127', NULL,        NULL, interval '27 days', NULL, interval '27 days'),
    -- Rosa: usó su prueba y luego un Mensual; ambos vencieron → volvió a Gratuito.
    ('rosa.chavez@demo.chilldrive.pe',   'Prueba',     30, -80, -50,   0.00, 'Gratis',   'Vencida',   NULL,       NULL,        NULL, interval '80 days', NULL, interval '80 days'),
    ('rosa.chavez@demo.chilldrive.pe',   'Mensual',    30, -35,  -5,  15.00, 'Yape',     'Vencida',   '37755310', NULL,        NULL, interval '35 days', NULL, interval '35 days'),
    -- Pedro: su pago no se pudo verificar → la app le muestra el motivo.
    ('pedro.salas@demo.chilldrive.pe',   'Mensual',    30,   0,  30,  15.00, 'Yape',     'Rechazada', '12345',    NULL,        NULL, NULL,
        'No encontramos un pago con ese número de operación. Revisa la constancia de Yape.', interval '1 day'),
    -- Ana: canjeó 1 500 puntos por un mes.
    ('ana.vargas@demo.chilldrive.pe',    'Puntos',     30,  -5,  25,   0.00, 'Puntos',   'Activa',    NULL,       NULL,        1500, interval '5 days',  NULL, interval '5 days')
) AS s(correo, plan, dias, ini, fin, monto, metodo, estado, ref, codigo, puntos, aprobado_hace, motivo, registrado_hace)
JOIN usuarios u ON u.correo = s.correo;

COMMIT;

-- Verificación rápida: un estado por conductor.
SELECT u.nombres || ' ' || u.apellidos AS conductor, u.tipo_plan, s.plan, s.metodo_pago, s.estado,
       s.fecha_fin - CURRENT_DATE AS dias_restantes
FROM usuarios u
LEFT JOIN suscripciones s ON s.usuario_id = u.id
WHERE u.correo LIKE '%@demo.chilldrive.pe'
ORDER BY u.id, s.fecha_registro;
