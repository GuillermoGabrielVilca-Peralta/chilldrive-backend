-- ChillDrive — flota de DEMOSTRACIÓN para presentaciones.
--
-- Crea 13 conductores ficticios con todo lo que el sistema muestra:
--   vehículo + cámara, ~2 meses de viajes finalizados con eventos de la IA (con
--   coordenadas en Lima), puntaje por viaje, libro de puntos, nivel/ranking y
--   suscripciones en todos los estados del ciclo de pago.
--
--   Contraseña de TODAS las cuentas demo:  Demo1234!
--   Teléfonos 9000000xx: así se reconocen y se borran (db/demo/02_limpiar_demo.sql).
--
-- Los viajes EN VIVO del mapa no se guardan aquí (desaparecen a los 10 min):
-- los genera el simulador  Panel/scripts/simular-flota.mjs  durante la presentación.
--
-- Fechas relativas al día en que se ejecuta. Idempotente: borra la demo anterior.
-- Requiere db/2026-10-27_suscripciones_pagos.sql y db/2026-10-27_viajes_ultima_ubicacion.sql.

BEGIN;

-- ── Limpieza de una corrida anterior (incluye la demo vieja @demo.chilldrive.pe) ──
CREATE TEMP TABLE demo_ids ON COMMIT DROP AS
SELECT id FROM usuarios
WHERE telefono LIKE '9000000__' OR correo LIKE '%@demo.chilldrive.pe';

DELETE FROM movimientos_puntos WHERE usuario_id IN (SELECT id FROM demo_ids);
DELETE FROM eventos WHERE viaje_id IN (SELECT id FROM viajes WHERE usuario_id IN (SELECT id FROM demo_ids));
DELETE FROM viajes WHERE usuario_id IN (SELECT id FROM demo_ids);
DELETE FROM canjes WHERE usuario_id IN (SELECT id FROM demo_ids);
-- suscripciones, puntos_usuario, vehículos y cámaras se borran en cascada con el usuario.
DELETE FROM usuarios WHERE id IN (SELECT id FROM demo_ids);

-- ── Conductores ─────────────────────────────────────────────────────────────
-- riesgo = probabilidad de que un evento sea una infracción (define el ranking).
CREATE TEMP TABLE demo_conductores ON COMMIT DROP AS
SELECT * FROM (VALUES
    ('Carla',    'Quispe Mamani',    'carlaquispe92@gmail.com',       '900000001', 'Particular', 'Gratuito',    'BKR-482', 'Toyota',     'Yaris',     2019, 'Blanco',   'Auto',  -12.0464, -77.0428, 34, 0.10, 75),
    ('Diego',    'Ramos Flores',     'diego.ramosf@hotmail.com',      '900000002', 'Taxi',       'Profesional', 'C4T-719', 'Hyundai',    'Accent',    2018, 'Amarillo', 'Taxi',  -12.1211, -77.0297, 14, 0.18, 12),
    ('Lucía',    'Torres Vega',      'luciatorres.v@gmail.com',       '900000003', 'Delivery',   'Gratuito',    '4821-LB', 'Honda',      'CB190R',    2021, 'Rojo',     'Moto',  -12.0890, -77.0505, 18, 0.20, 40),
    ('Marco',    'Huamán Ccori',     'marcohuaman07@gmail.com',       '900000004', 'Mototaxi',   'Gratuito',    '7315-MC', 'Bajaj',      'RE 205',    2020, 'Azul',     'Moto',  -11.9900, -77.0600, 16, 0.26, 35),
    ('Sofía',    'Mendoza Ríos',     'sofia.mendoza.rios@outlook.com','900000005', 'Taxi',       'Profesional', 'D7F-305', 'Kia',        'Rio',       2020, 'Plata',    'Taxi',  -12.0970, -77.0365, 42, 0.08, 70),
    ('Jorge',    'Paredes León',     'jparedesleon@gmail.com',        '900000006', 'Delivery',   'Profesional', '6092-JP', 'Yamaha',     'FZ 2.0',    2022, 'Negro',    'Moto',  -12.0560, -77.0844, 30, 0.16, 65),
    ('Rosa',     'Chávez Gutiérrez', 'rosachavez.g@hotmail.com',      '900000007', 'Particular', 'Gratuito',    'AWQ-157', 'Nissan',     'Versa',     2017, 'Gris',     'Auto',  -12.1450, -76.9900, 20, 0.19, 85),
    ('Pedro',    'Salas Núñez',      'pedrosalas.taxi@gmail.com',     '900000008', 'Taxi',       'Gratuito',    'C9L-864', 'Chevrolet',  'Sail',      2016, 'Amarillo', 'Taxi',  -12.0300, -77.0150, 12, 0.33, 30),
    ('Ana',      'Vargas Soto',      'anavargas.soto@gmail.com',      '900000009', 'Particular', 'Profesional', 'BTZ-640', 'Suzuki',     'Swift',     2021, 'Rojo',     'Auto',  -12.1100, -77.0050, 28, 0.11, 80),
    ('Luis',     'Fernández Paz',    'luisfernandez.paz@gmail.com',   '900000010', 'Taxi',       'Profesional', 'D2M-938', 'Toyota',     'Corolla',   2019, 'Blanco',   'Taxi',  -12.0750, -77.0700, 40, 0.09, 140),
    ('Milagros', 'Rojas Díaz',       'milagros.rojasd@hotmail.com',   '900000011', 'Delivery',   'Profesional', '5547-MR', 'Honda',      'Wave 110',  2022, 'Negro',    'Moto',  -12.1600, -77.0200, 26, 0.14, 50),
    ('Kevin',    'Castillo Lázaro',  'kevincastillo.l@gmail.com',     '900000012', 'Mototaxi',   'Profesional', '8126-KC', 'TVS',        'King',      2021, 'Verde',    'Moto',  -11.9650, -77.0700, 22, 0.21, 45),
    ('Valeria',  'Gómez Paucar',     'valegomez.taxi@outlook.com',    '900000013', 'Taxi',       'Profesional', 'C6V-271', 'Kia',        'Soluto',    2023, 'Amarillo', 'Taxi',  -12.0850, -76.9750, 44, 0.06, 120)
) AS d(nombres, apellidos, correo, telefono, tipo_conductor, tipo_plan, placa, marca, modelo, anio, color,
       tipo_vehiculo, base_lat, base_lon, viajes, riesgo, antiguedad_dias);

-- Hash BCrypt (factor 12, igual que PasswordService) de "Demo1234!".
INSERT INTO usuarios (nombres, apellidos, correo, password_hash, telefono, tipo_plan, tipo_conductor, estado, fecha_registro)
SELECT nombres, apellidos, correo, '$2a$12$vYIauyQmZVRn7hZA9qKMte159Z2a/67htDmAf0.YDKsb/Tus16vMS', telefono,
       tipo_plan, tipo_conductor, 'Activo', now() - make_interval(days => antiguedad_dias)
FROM demo_conductores;

INSERT INTO vehiculos (usuario_id, placa, marca, modelo, anio, color, tipo_vehiculo, estado, fecha_registro)
SELECT u.id, d.placa, d.marca, d.modelo, d.anio, d.color, d.tipo_vehiculo, 'Activo', u.fecha_registro + interval '20 minutes'
FROM demo_conductores d JOIN usuarios u ON u.correo = d.correo;

INSERT INTO camaras (vehiculo_id, nombre, posicion, estado, fecha_registro)
SELECT v.id, 'Cámara del celular', 'Frontal', 'Conectada', v.fecha_registro + interval '5 minutes'
FROM vehiculos v JOIN usuarios u ON u.id = v.usuario_id JOIN demo_conductores d ON d.correo = u.correo;

-- ── Viajes finalizados (entre 6:00 y 22:00 hora de Lima; se guardan en UTC = Lima + 5 h) ──
SELECT setseed(0.42);

INSERT INTO viajes (usuario_id, vehiculo_id, fecha_inicio, fecha_fin, distancia_km, duracion_minutos, estado)
SELECT u.id, v.id, t.inicio, t.inicio + make_interval(mins => t.minutos),
       round((t.minutos * (18 + random() * 17) / 60.0)::numeric, 2), t.minutos, 'Finalizado'
FROM demo_conductores d
JOIN usuarios u  ON u.correo = d.correo
JOIN vehiculos v ON v.usuario_id = u.id
CROSS JOIN LATERAL generate_series(1, d.viajes) g
CROSS JOIN LATERAL (
    SELECT date_trunc('day', now() - make_interval(days => 2 + floor(random() * least(d.antiguedad_dias - 2, 60))::int))
               + make_interval(hours => 11 + floor(random() * 16)::int, mins => floor(random() * 60)::int) AS inicio,
           (12 + floor(random() * 70))::int AS minutos
    WHERE g > 0
) t;

-- ── Eventos de la IA durante cada viaje ─────────────────────────────────────
INSERT INTO eventos (viaje_id, camara_id, tipo_evento, impacto_puntaje, latitud, longitud, fecha_evento)
SELECT vj.id, c.id,
       CASE WHEN random() < d.riesgo
            THEN (ARRAY['Frenado Brusco','Aceleración Brusca','Giro Brusco','Exceso Velocidad','Uso Celular'])[1 + floor(random() * 5)::int]
            ELSE (ARRAY['Señal Respetada','Carril Mantenido','Distancia Segura','Carril Mantenido','Distancia Segura','Peaton Detectado'])[1 + floor(random() * 6)::int]
       END,
       0,
       round((d.base_lat + (random() - 0.5) * 0.06)::numeric, 7),
       round((d.base_lon + (random() - 0.5) * 0.06)::numeric, 7),
       vj.fecha_inicio + make_interval(secs => random() * vj.duracion_minutos * 60)
FROM viajes vj
JOIN usuarios u  ON u.id = vj.usuario_id
JOIN demo_conductores d ON d.correo = u.correo
JOIN camaras c   ON c.vehiculo_id = vj.vehiculo_id
CROSS JOIN LATERAL generate_series(1, 4 + floor(random() * 12)::int + vj.id % 1) e;

-- Mismos valores que Models/Constants/ImpactoEventos.cs.
UPDATE eventos e SET impacto_puntaje = CASE e.tipo_evento
        WHEN 'Señal Respetada'    THEN  10
        WHEN 'Carril Mantenido'   THEN   5
        WHEN 'Distancia Segura'   THEN   5
        WHEN 'Frenado Brusco'     THEN -10
        WHEN 'Aceleración Brusca' THEN  -8
        WHEN 'Giro Brusco'        THEN  -8
        WHEN 'Uso Celular'        THEN -15
        WHEN 'Exceso Velocidad'   THEN -25
        ELSE 0 END
FROM viajes v JOIN usuarios u ON u.id = v.usuario_id
WHERE e.viaje_id = v.id AND u.telefono LIKE '9000000__';

-- Puntaje del viaje: misma fórmula que ScoreConduccion.General (promedio de 5 categorías).
UPDATE viajes v SET
    puntaje_conduccion = (
        SELECT floor(avg(greatest(0, 100 + coalesce((
                   SELECT sum(e.impacto_puntaje) FROM eventos e
                   WHERE e.viaje_id = v.id AND e.tipo_evento = t AND e.impacto_puntaje < 0), 0))))
        FROM unnest(ARRAY['Exceso Velocidad','Frenado Brusco','Aceleración Brusca','Giro Brusco','Uso Celular']) t),
    puntos_ganados = greatest(0, (SELECT coalesce(sum(e.impacto_puntaje), 0) FROM eventos e WHERE e.viaje_id = v.id))
FROM usuarios u
WHERE u.id = v.usuario_id AND u.telefono LIKE '9000000__';

-- ── Libro de puntos: una fila por evento, igual que EventoService ───────────
INSERT INTO movimientos_puntos (usuario_id, tipo, puntos, descripcion, viaje_id, fecha_movimiento)
SELECT v.usuario_id, CASE WHEN e.impacto_puntaje > 0 THEN 'Ganado' ELSE 'Ajuste' END, e.impacto_puntaje,
       format('Evento ''%s'' en viaje #%s', e.tipo_evento, v.id), v.id, e.fecha_evento
FROM eventos e
JOIN viajes v   ON v.id = e.viaje_id
JOIN usuarios u ON u.id = v.usuario_id
WHERE u.telefono LIKE '9000000__' AND e.impacto_puntaje <> 0;

-- Bono de referidos para que Carla pueda pagar con puntos y Ana haya podido hacerlo.
INSERT INTO movimientos_puntos (usuario_id, tipo, puntos, descripcion, fecha_movimiento)
SELECT u.id, 'Ajuste', b.meta - coalesce(sum(m.puntos), 0), 'Bono por referir conductores', now() - interval '6 days'
FROM (VALUES ('carlaquispe92@gmail.com', 1850), ('anavargas.soto@gmail.com', 1820)) AS b(correo, meta)
JOIN usuarios u ON u.correo = b.correo
LEFT JOIN movimientos_puntos m ON m.usuario_id = u.id
GROUP BY u.id, b.meta
HAVING coalesce(sum(m.puntos), 0) < b.meta;

INSERT INTO movimientos_puntos (usuario_id, tipo, puntos, descripcion, fecha_movimiento)
SELECT u.id, 'Canjeado', -1500, 'Plan Profesional por 30 días pagado con puntos', now() - interval '5 days'
FROM usuarios u WHERE u.correo = 'anavargas.soto@gmail.com';

-- Saldo y nivel a partir del libro (mismos umbrales que NivelPuntos.Calcular).
INSERT INTO puntos_usuario (usuario_id, puntos_actuales, puntos_historicos, nivel)
SELECT u.id, greatest(0, s.saldo), s.historico,
       CASE WHEN s.historico >= 5000 THEN 'Diamante' WHEN s.historico >= 2000 THEN 'Platino'
            WHEN s.historico >= 800  THEN 'Oro'      WHEN s.historico >= 300  THEN 'Plata' ELSE 'Bronce' END
FROM usuarios u
CROSS JOIN LATERAL (
    SELECT coalesce(sum(m.puntos), 0)::int AS saldo,
           coalesce(sum(m.puntos) FILTER (WHERE m.puntos > 0), 0)::int AS historico
    FROM movimientos_puntos m WHERE m.usuario_id = u.id
) s
WHERE u.telefono LIKE '9000000__';

UPDATE usuarios u SET ultimo_acceso = (SELECT max(v.fecha_fin) FROM viajes v WHERE v.usuario_id = u.id)
WHERE u.telefono LIKE '9000000__';

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
    ('diego.ramosf@hotmail.com',        'Prueba',     30, -10,  20,   0.00, 'Gratis',   'Activa',    NULL,       NULL,        NULL, interval '10 days',  NULL, interval '10 days'),
    -- Lucía: yapeó el plan Mensual y espera verificación → "Pagos por verificar".
    ('luciatorres.v@gmail.com',         'Mensual',    30,   0,  30,  15.00, 'Yape',     'Pendiente', '40817352', NULL,        NULL, NULL,                NULL, interval '2 hours'),
    -- Marco: pagará el Trimestral en efectivo con su código.
    ('marcohuaman07@gmail.com',         'Trimestral', 120,  0, 120,  45.00, 'Efectivo', 'Pendiente', NULL,       'CD-7K3QXP', NULL, NULL,                NULL, interval '1 day'),
    -- Sofía: plan Anual aprobado hace un mes.
    ('sofia.mendoza.rios@outlook.com',  'Anual',      365, -30, 335, 150.00, 'Yape',    'Activa',    '39120455', NULL,        NULL, interval '30 days',  NULL, interval '30 days'),
    -- Jorge: Mensual que vence en 3 días → aviso "por vencer" y recordatorio.
    ('jparedesleon@gmail.com',          'Mensual',    30, -27,   3,  15.00, 'Yape',     'Activa',    '38800127', NULL,        NULL, interval '27 days',  NULL, interval '27 days'),
    -- Rosa: usó su prueba y luego un Mensual; ambos vencieron → volvió a Gratuito.
    ('rosachavez.g@hotmail.com',        'Prueba',     30, -80, -50,   0.00, 'Gratis',   'Vencida',   NULL,       NULL,        NULL, interval '80 days',  NULL, interval '80 days'),
    ('rosachavez.g@hotmail.com',        'Mensual',    30, -35,  -5,  15.00, 'Yape',     'Vencida',   '37755310', NULL,        NULL, interval '35 days',  NULL, interval '35 days'),
    -- Pedro: su pago no se pudo verificar → la app le muestra el motivo.
    ('pedrosalas.taxi@gmail.com',       'Mensual',    30,   0,  30,  15.00, 'Yape',     'Rechazada', '12345',    NULL,        NULL, NULL,
        'No encontramos un pago con ese número de operación. Revisa la constancia de Yape.', interval '1 day'),
    -- Ana: canjeó 1 500 puntos por un mes.
    ('anavargas.soto@gmail.com',        'Puntos',     30,  -5,  25,   0.00, 'Puntos',   'Activa',    NULL,       NULL,        1500, interval '5 days',   NULL, interval '5 days'),
    -- Luis: probó gratis, luego compró el Trimestral (promo "1 mes gratis").
    ('luisfernandez.paz@gmail.com',     'Prueba',     30, -130, -100, 0.00, 'Gratis',   'Vencida',   NULL,       NULL,        NULL, interval '130 days', NULL, interval '130 days'),
    ('luisfernandez.paz@gmail.com',     'Trimestral', 120, -40,  80,  45.00, 'Yape',    'Activa',    '41022968', NULL,        NULL, interval '40 days',  NULL, interval '40 days'),
    -- Milagros: pagó el Mensual en efectivo en oficina.
    ('milagros.rojasd@hotmail.com',     'Mensual',    30, -12,  18,  15.00, 'Efectivo', 'Activa',    NULL,       'CD-4M8TZW', NULL, interval '12 days',  NULL, interval '12 days'),
    -- Kevin: renovó su Mensual hace 3 días.
    ('kevincastillo.l@gmail.com',       'Mensual',    30, -33,  -3,  15.00, 'Yape',     'Vencida',   '39877410', NULL,        NULL, interval '33 days',  NULL, interval '33 days'),
    ('kevincastillo.l@gmail.com',       'Mensual',    30,  -3,  27,  15.00, 'Yape',     'Activa',    '41390025', NULL,        NULL, interval '3 days',   NULL, interval '3 days'),
    -- Valeria: plan Anual (promo "2 meses gratis") desde hace 100 días.
    ('valegomez.taxi@outlook.com',      'Anual',      365, -100, 265, 150.00, 'Yape',   'Activa',    '36541187', NULL,        NULL, interval '100 days', NULL, interval '100 days')
) AS s(correo, plan, dias, ini, fin, monto, metodo, estado, ref, codigo, puntos, aprobado_hace, motivo, registrado_hace)
JOIN usuarios u ON u.correo = s.correo;

COMMIT;

-- Verificación rápida: un renglón por conductor.
SELECT u.nombres || ' ' || u.apellidos AS conductor, u.tipo_conductor, u.tipo_plan,
       (SELECT count(*) FROM viajes v WHERE v.usuario_id = u.id) AS viajes,
       (SELECT round(avg(v.puntaje_conduccion)) FROM viajes v WHERE v.usuario_id = u.id) AS puntaje_prom,
       p.puntos_actuales, p.nivel,
       (SELECT string_agg(s.plan || ' ' || s.estado, ', ' ORDER BY s.fecha_registro) FROM suscripciones s WHERE s.usuario_id = u.id) AS suscripciones
FROM usuarios u JOIN puntos_usuario p ON p.usuario_id = u.id
WHERE u.telefono LIKE '9000000__'
ORDER BY p.puntos_historicos DESC;
