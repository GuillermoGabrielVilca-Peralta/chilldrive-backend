-- ChillDrive — flota de DEMOSTRACIÓN para presentaciones.
--
-- Crea 13 conductores ficticios con todo lo que el sistema muestra:
--   vehículo + cámara, ~2 meses de viajes finalizados con eventos de la IA (con
--   coordenadas en la ciudad de Arequipa), puntaje por viaje, libro de puntos, nivel/ranking y
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
-- rutas = avenidas por donde suele circular (ver demo_puntos).
CREATE TEMP TABLE demo_conductores ON COMMIT DROP AS
SELECT * FROM (VALUES
    ('Carla',    'Quispe Mamani',    'carlaquispe92@gmail.com',       '900000001', 'Particular', 'Gratuito',    'VBK-482', 'Toyota',     'Yaris',     2019, 'Blanco',   'Auto',  ARRAY['ejercito','cerroColorado'], 34, 0.10, 75),
    ('Diego',    'Ramos Flores',     'diego.ramosf@hotmail.com',      '900000002', 'Taxi',       'Profesional', 'V4T-719', 'Hyundai',    'Accent',    2018, 'Amarillo', 'Taxi',  ARRAY['venezuela','uchumayo','parra'], 14, 0.18, 12),
    ('Lucía',    'Torres Vega',      'luciatorres.v@gmail.com',       '900000003', 'Delivery',   'Gratuito',    '4821-LB', 'Honda',      'CB190R',    2021, 'Rojo',     'Moto',  ARRAY['cerroColorado','aviacion','ejercito'], 18, 0.20, 40),
    ('Marco',    'Huamán Ccori',     'marcohuaman07@gmail.com',       '900000004', 'Mototaxi',   'Gratuito',    '7315-MC', 'Bajaj',      'RE 205',    2020, 'Azul',     'Moto',  ARRAY['aviacion','cerroColorado'], 16, 0.26, 35),
    ('Sofía',    'Mendoza Ríos',     'sofia.mendoza.rios@outlook.com','900000005', 'Taxi',       'Profesional', 'V7F-305', 'Kia',        'Rio',       2020, 'Plata',    'Taxi',  ARRAY['dolores','lambramani','parra'], 42, 0.08, 70),
    ('Jorge',    'Paredes León',     'jparedesleon@gmail.com',        '900000006', 'Delivery',   'Profesional', '6092-JP', 'Yamaha',     'FZ 2.0',    2022, 'Negro',    'Moto',  ARRAY['jesus','dolores','lambramani'], 30, 0.16, 65),
    ('Rosa',     'Chávez Gutiérrez', 'rosachavez.g@hotmail.com',      '900000007', 'Particular', 'Gratuito',    'VAW-157', 'Nissan',     'Versa',     2017, 'Gris',     'Auto',  ARRAY['uchumayo','hunter'], 20, 0.19, 85),
    ('Pedro',    'Salas Núñez',      'pedrosalas.taxi@gmail.com',     '900000008', 'Taxi',       'Gratuito',    'V9L-864', 'Chevrolet',  'Sail',      2016, 'Amarillo', 'Taxi',  ARRAY['jesus','selvaAlegre'], 12, 0.33, 30),
    ('Ana',      'Vargas Soto',      'anavargas.soto@gmail.com',      '900000009', 'Particular', 'Profesional', 'VTZ-640', 'Suzuki',     'Swift',     2021, 'Rojo',     'Auto',  ARRAY['selvaAlegre','jesus'], 28, 0.11, 80),
    ('Luis',     'Fernández Paz',    'luisfernandez.paz@gmail.com',   '900000010', 'Taxi',       'Profesional', 'V2M-938', 'Toyota',     'Corolla',   2019, 'Blanco',   'Taxi',  ARRAY['selvaAlegre','ejercito','venezuela'], 40, 0.09, 140),
    ('Milagros', 'Rojas Díaz',       'milagros.rojasd@hotmail.com',   '900000011', 'Delivery',   'Profesional', '5547-MR', 'Honda',      'Wave 110',  2022, 'Negro',    'Moto',  ARRAY['socabaya','hunter','lambramani'], 26, 0.14, 50),
    ('Kevin',    'Castillo Lázaro',  'kevincastillo.l@gmail.com',     '900000012', 'Mototaxi',   'Profesional', '8126-KC', 'TVS',        'King',      2021, 'Verde',    'Moto',  ARRAY['hunter','uchumayo','socabaya'], 22, 0.21, 45),
    ('Valeria',  'Gómez Paucar',     'valegomez.taxi@outlook.com',    '900000013', 'Taxi',       'Profesional', 'V6C-271', 'Kia',        'Soluto',    2023, 'Amarillo', 'Taxi',  ARRAY['ejercito','venezuela','parra'], 44, 0.06, 120)
) AS d(nombres, apellidos, correo, telefono, tipo_conductor, tipo_plan, placa, marca, modelo, anio, color,
       tipo_vehiculo, rutas, viajes, riesgo, antiguedad_dias);

-- Puntos cada ~100 m sobre calles reales de Arequipa (trazado de OSRM/OpenStreetMap,
-- mismas rutas que Panel/scripts/rutas-arequipa.json): los eventos caen sobre la pista.
CREATE TEMP TABLE demo_puntos ON COMMIT DROP AS
SELECT * FROM (VALUES
    ('ejercito',-16.395605,-71.541921),('ejercito',-16.394774,-71.541853),('ejercito',-16.393814,-71.541365),('ejercito',-16.392953,-71.541077),('ejercito',-16.392124,-71.540572),('ejercito',-16.391799,-71.541452),
    ('ejercito',-16.391476,-71.542328),('ejercito',-16.391100,-71.543342),('ejercito',-16.390744,-71.544301),('ejercito',-16.390334,-71.545478),('ejercito',-16.389990,-71.546503),('ejercito',-16.389604,-71.547440),
    ('ejercito',-16.388746,-71.547326),('ejercito',-16.387538,-71.546993),('ejercito',-16.386456,-71.546695),('ejercito',-16.385408,-71.546406),('ejercito',-16.384392,-71.546126),('ejercito',-16.383303,-71.545826),
    ('ejercito',-16.382274,-71.545528),('ejercito',-16.381305,-71.545295),('ejercito',-16.379965,-71.544952),('ejercito',-16.378898,-71.544689),('ejercito',-16.377982,-71.544695),('ejercito',-16.377023,-71.544707),
    ('ejercito',-16.375793,-71.544671),('ejercito',-16.374435,-71.544492),('ejercito',-16.373248,-71.544444),('ejercito',-16.371759,-71.544504),('ejercito',-16.370746,-71.544561),('ejercito',-16.369562,-71.544612),
    ('ejercito',-16.367849,-71.544219),('ejercito',-16.366927,-71.544064),('ejercito',-16.366351,-71.545037),('ejercito',-16.365652,-71.545679),('ejercito',-16.365035,-71.546884),('ejercito',-16.365939,-71.547852),
    ('ejercito',-16.366479,-71.548649),('ejercito',-16.367452,-71.549088),('ejercito',-16.368286,-71.550459),('ejercito',-16.368518,-71.552626),('ejercito',-16.368050,-71.553485),('ejercito',-16.368682,-71.553177),
    ('ejercito',-16.369450,-71.553202),('aviacion',-16.370165,-71.556091),('aviacion',-16.370710,-71.557693),('aviacion',-16.371924,-71.557997),('aviacion',-16.373305,-71.558270),('aviacion',-16.373098,-71.559089),
    ('aviacion',-16.371918,-71.559376),('aviacion',-16.370719,-71.559662),('aviacion',-16.369478,-71.559947),('aviacion',-16.367387,-71.560442),('aviacion',-16.366390,-71.560683),('aviacion',-16.364859,-71.561040),
    ('aviacion',-16.363225,-71.561431),('aviacion',-16.362132,-71.561678),('aviacion',-16.360794,-71.562017),('aviacion',-16.359941,-71.562354),('aviacion',-16.358852,-71.562612),('aviacion',-16.355426,-71.563413),
    ('aviacion',-16.354576,-71.564554),('aviacion',-16.353766,-71.565278),('aviacion',-16.353057,-71.565884),('aviacion',-16.352278,-71.566563),('aviacion',-16.351562,-71.567215),('aviacion',-16.349362,-71.565138),
    ('aviacion',-16.348724,-71.564422),('aviacion',-16.347793,-71.564064),('aviacion',-16.346923,-71.564268),('aviacion',-16.345646,-71.565143),('aviacion',-16.345840,-71.566359),('aviacion',-16.345771,-71.567684),
    ('aviacion',-16.345538,-71.568580),('parra',-16.403450,-71.538077),('parra',-16.402872,-71.537096),('parra',-16.403379,-71.535733),('parra',-16.403759,-71.534695),('parra',-16.404693,-71.535043),
    ('parra',-16.405554,-71.535435),('parra',-16.406349,-71.535068),('parra',-16.407344,-71.534273),('parra',-16.408719,-71.534251),('parra',-16.409811,-71.534449),('parra',-16.410665,-71.535165),
    ('parra',-16.411378,-71.534651),('parra',-16.412090,-71.535296),('parra',-16.412807,-71.535259),('parra',-16.413588,-71.535542),('parra',-16.414259,-71.536896),('parra',-16.414604,-71.537987),
    ('parra',-16.414793,-71.539051),('parra',-16.415438,-71.539435),('parra',-16.416616,-71.539354),('parra',-16.417142,-71.540597),('parra',-16.417676,-71.540334),('parra',-16.418371,-71.539576),
    ('parra',-16.419074,-71.538963),('parra',-16.420077,-71.538258),('parra',-16.421522,-71.537858),('parra',-16.422506,-71.537587),('parra',-16.423418,-71.537438),('parra',-16.424498,-71.537486),
    ('parra',-16.425230,-71.538042),('parra',-16.425704,-71.538960),('parra',-16.426370,-71.539788),('parra',-16.427831,-71.538669),('parra',-16.428527,-71.538028),('parra',-16.429183,-71.538039),
    ('parra',-16.429901,-71.539161),('parra',-16.428619,-71.540343),('dolores',-16.407969,-71.525896),('dolores',-16.407237,-71.524723),('dolores',-16.406776,-71.523547),('dolores',-16.406200,-71.523384),
    ('dolores',-16.406436,-71.523184),('dolores',-16.406962,-71.524527),('dolores',-16.407715,-71.524329),('dolores',-16.408531,-71.523907),('dolores',-16.409554,-71.523433),('dolores',-16.410240,-71.522755),
    ('dolores',-16.410924,-71.521956),('dolores',-16.411569,-71.521400),('dolores',-16.412251,-71.520782),('dolores',-16.413412,-71.520083),('dolores',-16.414637,-71.519423),('dolores',-16.415835,-71.518831),
    ('dolores',-16.416669,-71.518428),('dolores',-16.417628,-71.517977),('dolores',-16.418497,-71.517535),('dolores',-16.419370,-71.517358),('dolores',-16.419576,-71.518340),('dolores',-16.419712,-71.519483),
    ('dolores',-16.418927,-71.519866),('dolores',-16.417944,-71.520194),('dolores',-16.418367,-71.519482),('dolores',-16.419479,-71.519266),('dolores',-16.419576,-71.518340),('dolores',-16.419370,-71.517358),
    ('dolores',-16.419832,-71.516728),('dolores',-16.421080,-71.516527),('dolores',-16.422256,-71.516398),('dolores',-16.423159,-71.516067),('dolores',-16.424020,-71.515743),('dolores',-16.425006,-71.515313),
    ('dolores',-16.425941,-71.515600),('dolores',-16.426588,-71.517198),('dolores',-16.426901,-71.518107),('dolores',-16.427374,-71.517876),('dolores',-16.428287,-71.516116),('dolores',-16.428258,-71.515242),
    ('jesus',-16.405981,-71.524981),('jesus',-16.405562,-71.525609),('jesus',-16.406390,-71.525337),('jesus',-16.407069,-71.524722),('jesus',-16.407883,-71.524221),('jesus',-16.408964,-71.523709),
    ('jesus',-16.409956,-71.523081),('jesus',-16.410924,-71.521956),('jesus',-16.411569,-71.521400),('jesus',-16.411196,-71.520535),('jesus',-16.410923,-71.519525),('jesus',-16.410750,-71.518484),
    ('jesus',-16.410785,-71.517505),('jesus',-16.410868,-71.516395),('jesus',-16.410924,-71.515311),('jesus',-16.410970,-71.514234),('jesus',-16.410998,-71.513206),('jesus',-16.411968,-71.513171),
    ('jesus',-16.413018,-71.513741),('jesus',-16.413237,-71.512677),('jesus',-16.413656,-71.511921),('jesus',-16.413452,-71.510897),('jesus',-16.414024,-71.510056),('jesus',-16.414692,-71.509040),
    ('jesus',-16.415491,-71.507879),('jesus',-16.416217,-71.506771),('jesus',-16.416766,-71.505949),('jesus',-16.417359,-71.505061),('jesus',-16.417896,-71.504265),('jesus',-16.418645,-71.503152),
    ('jesus',-16.419233,-71.502279),('jesus',-16.419352,-71.500614),('venezuela',-16.407937,-71.545357),('venezuela',-16.406988,-71.545172),('venezuela',-16.405393,-71.545016),('venezuela',-16.404683,-71.544274),
    ('venezuela',-16.405182,-71.543410),('venezuela',-16.405740,-71.542494),('venezuela',-16.406446,-71.541894),('venezuela',-16.407365,-71.542158),('venezuela',-16.408229,-71.541816),('venezuela',-16.408599,-71.541009),
    ('venezuela',-16.407899,-71.540366),('venezuela',-16.406904,-71.539466),('venezuela',-16.407514,-71.538821),('venezuela',-16.408266,-71.538259),('venezuela',-16.408986,-71.537430),('venezuela',-16.408404,-71.536693),
    ('venezuela',-16.407672,-71.535788),('venezuela',-16.406830,-71.534739),('venezuela',-16.407288,-71.533565),('venezuela',-16.406609,-71.532660),('venezuela',-16.405839,-71.531657),('venezuela',-16.405642,-71.530933),
    ('venezuela',-16.405970,-71.529962),('venezuela',-16.405632,-71.529010),('venezuela',-16.404902,-71.528116),('venezuela',-16.404274,-71.527350),('venezuela',-16.402982,-71.525769),('venezuela',-16.402397,-71.525050),
    ('venezuela',-16.401748,-71.524249),('venezuela',-16.403023,-71.523092),('venezuela',-16.403159,-71.522139),('venezuela',-16.402542,-71.521447),('venezuela',-16.403497,-71.519929),('venezuela',-16.404289,-71.519439),
    ('venezuela',-16.405296,-71.518984),('venezuela',-16.406328,-71.518529),('uchumayo',-16.410106,-71.550058),('uchumayo',-16.410738,-71.551034),('uchumayo',-16.411199,-71.551902),('uchumayo',-16.411939,-71.552867),
    ('uchumayo',-16.412781,-71.553531),('uchumayo',-16.413457,-71.554168),('uchumayo',-16.414371,-71.554573),('uchumayo',-16.415314,-71.555206),('uchumayo',-16.416103,-71.555782),('uchumayo',-16.416828,-71.556461),
    ('uchumayo',-16.417601,-71.557015),('uchumayo',-16.418383,-71.557658),('uchumayo',-16.419098,-71.558719),('uchumayo',-16.420129,-71.559465),('uchumayo',-16.420900,-71.560414),('uchumayo',-16.421401,-71.561547),
    ('uchumayo',-16.422078,-71.562209),('uchumayo',-16.422677,-71.563100),('uchumayo',-16.423230,-71.563925),('uchumayo',-16.422511,-71.564565),('uchumayo',-16.423414,-71.565455),('uchumayo',-16.423736,-71.566358),
    ('uchumayo',-16.424338,-71.567177),('uchumayo',-16.424483,-71.568088),('uchumayo',-16.425396,-71.568306),('uchumayo',-16.426400,-71.568525),('uchumayo',-16.427734,-71.569218),('uchumayo',-16.428567,-71.569843),
    ('uchumayo',-16.429480,-71.570165),('uchumayo',-16.429663,-71.571373),('uchumayo',-16.429841,-71.574402),('lambramani',-16.408139,-71.529716),('lambramani',-16.408937,-71.529244),('lambramani',-16.410826,-71.528755),
    ('lambramani',-16.411982,-71.528455),('lambramani',-16.412879,-71.528119),('lambramani',-16.413867,-71.528004),('lambramani',-16.414598,-71.528774),('lambramani',-16.415143,-71.529592),('lambramani',-16.415734,-71.530587),
    ('lambramani',-16.416284,-71.531491),('lambramani',-16.416752,-71.532444),('lambramani',-16.417253,-71.533687),('lambramani',-16.417565,-71.534602),('lambramani',-16.417386,-71.533856),('lambramani',-16.417009,-71.532996),
    ('lambramani',-16.416340,-71.531458),('lambramani',-16.415811,-71.530526),('lambramani',-16.415309,-71.529256),('lambramani',-16.414704,-71.528812),('lambramani',-16.415143,-71.529592),('lambramani',-16.415734,-71.530587),
    ('lambramani',-16.416284,-71.531491),('lambramani',-16.416752,-71.532444),('lambramani',-16.417253,-71.533687),('lambramani',-16.417565,-71.534602),('lambramani',-16.418119,-71.535530),('lambramani',-16.419374,-71.537064),
    ('lambramani',-16.420037,-71.538033),('lambramani',-16.421522,-71.537858),('lambramani',-16.422506,-71.537587),('lambramani',-16.423418,-71.537438),('lambramani',-16.424498,-71.537486),('lambramani',-16.425052,-71.536775),
    ('lambramani',-16.425416,-71.535436),('lambramani',-16.426179,-71.535912),('socabaya',-16.425268,-71.530225),('socabaya',-16.425289,-71.531182),('socabaya',-16.425271,-71.532490),('socabaya',-16.425382,-71.533400),
    ('socabaya',-16.426783,-71.533172),('socabaya',-16.428575,-71.533172),('socabaya',-16.429598,-71.533099),('socabaya',-16.430500,-71.533046),('socabaya',-16.432419,-71.532305),('socabaya',-16.433722,-71.531827),
    ('socabaya',-16.434593,-71.531504),('socabaya',-16.435544,-71.531166),('socabaya',-16.436434,-71.530855),('socabaya',-16.437417,-71.530530),('socabaya',-16.438693,-71.530077),('socabaya',-16.439607,-71.529743),
    ('socabaya',-16.440633,-71.530222),('socabaya',-16.441914,-71.529748),('socabaya',-16.442235,-71.529095),('socabaya',-16.443018,-71.528997),('socabaya',-16.444393,-71.530125),('socabaya',-16.445271,-71.530369),
    ('socabaya',-16.446268,-71.530583),('socabaya',-16.447016,-71.531118),('socabaya',-16.447838,-71.531664),('socabaya',-16.448865,-71.532364),('socabaya',-16.449690,-71.532752),('socabaya',-16.450600,-71.532325),
    ('socabaya',-16.452347,-71.531196),('socabaya',-16.453307,-71.530597),('socabaya',-16.454335,-71.529922),('socabaya',-16.455379,-71.529409),('socabaya',-16.456329,-71.529188),('socabaya',-16.457435,-71.529095),
    ('socabaya',-16.458849,-71.528799),('socabaya',-16.459785,-71.528766),('socabaya',-16.460856,-71.528486),('socabaya',-16.461696,-71.527933),('socabaya',-16.462226,-71.527867),('socabaya',-16.461645,-71.528744),
    ('socabaya',-16.461067,-71.529673),('hunter',-16.420173,-71.545404),('hunter',-16.420982,-71.545390),('hunter',-16.422985,-71.547325),('hunter',-16.423939,-71.547631),('hunter',-16.425002,-71.546860),
    ('hunter',-16.425932,-71.546230),('hunter',-16.426908,-71.546415),('hunter',-16.427562,-71.547186),('hunter',-16.428047,-71.548567),('hunter',-16.428670,-71.549142),('hunter',-16.429582,-71.549300),
    ('hunter',-16.430525,-71.548807),('hunter',-16.431406,-71.549027),('hunter',-16.432076,-71.548502),('hunter',-16.431713,-71.547731),('hunter',-16.431765,-71.546910),('hunter',-16.432534,-71.546767),
    ('hunter',-16.433527,-71.546628),('hunter',-16.434389,-71.546509),('hunter',-16.435072,-71.545874),('hunter',-16.435843,-71.545195),('hunter',-16.436508,-71.544711),('hunter',-16.437408,-71.544479),
    ('hunter',-16.438134,-71.545046),('hunter',-16.438802,-71.545977),('hunter',-16.438898,-71.547147),('hunter',-16.439251,-71.548388),('hunter',-16.439720,-71.549314),('hunter',-16.439830,-71.550565),
    ('hunter',-16.440218,-71.552047),('hunter',-16.440615,-71.552902),('hunter',-16.441383,-71.553466),('hunter',-16.442142,-71.553111),('selvaAlegre',-16.399288,-71.537088),('selvaAlegre',-16.400145,-71.538001),
    ('selvaAlegre',-16.400545,-71.536967),('selvaAlegre',-16.400904,-71.535955),('selvaAlegre',-16.401286,-71.534880),('selvaAlegre',-16.400290,-71.534471),('selvaAlegre',-16.399243,-71.534049),('selvaAlegre',-16.398221,-71.533636),
    ('selvaAlegre',-16.397197,-71.533207),('selvaAlegre',-16.396225,-71.532800),('selvaAlegre',-16.395116,-71.532335),('selvaAlegre',-16.394244,-71.531955),('selvaAlegre',-16.393303,-71.531707),('selvaAlegre',-16.392422,-71.531516),
    ('selvaAlegre',-16.391752,-71.530779),('selvaAlegre',-16.391239,-71.529717),('selvaAlegre',-16.391128,-71.528760),('selvaAlegre',-16.390380,-71.527897),('selvaAlegre',-16.390239,-71.527809),('selvaAlegre',-16.390632,-71.528231),
    ('selvaAlegre',-16.389995,-71.527373),('selvaAlegre',-16.389357,-71.526530),('selvaAlegre',-16.388515,-71.525379),('selvaAlegre',-16.387897,-71.524533),('selvaAlegre',-16.386898,-71.523165),('selvaAlegre',-16.386333,-71.522392),
    ('selvaAlegre',-16.385699,-71.521524),('selvaAlegre',-16.385047,-71.520632),('selvaAlegre',-16.383998,-71.521362),('selvaAlegre',-16.382718,-71.520986),('selvaAlegre',-16.381977,-71.520020),('cerroColorado',-16.378134,-71.550294),
    ('cerroColorado',-16.379088,-71.550940),('cerroColorado',-16.380005,-71.551078),('cerroColorado',-16.380629,-71.551698),('cerroColorado',-16.380836,-71.552637),('cerroColorado',-16.380265,-71.553337),('cerroColorado',-16.379538,-71.554061),
    ('cerroColorado',-16.378989,-71.555038),('cerroColorado',-16.377907,-71.555828),('cerroColorado',-16.376804,-71.556745),('cerroColorado',-16.375771,-71.557458),('cerroColorado',-16.374829,-71.558274),('cerroColorado',-16.374020,-71.558847),
    ('cerroColorado',-16.373305,-71.558270),('cerroColorado',-16.371924,-71.557997),('cerroColorado',-16.370762,-71.557730),('cerroColorado',-16.370236,-71.556472),('cerroColorado',-16.369697,-71.555827),('cerroColorado',-16.368404,-71.553912),
    ('cerroColorado',-16.368682,-71.553177),('cerroColorado',-16.367919,-71.553736),('cerroColorado',-16.367010,-71.554118),('cerroColorado',-16.366021,-71.554299),('cerroColorado',-16.366246,-71.555258),('cerroColorado',-16.367387,-71.560442),
    ('cerroColorado',-16.366105,-71.561064),('cerroColorado',-16.364979,-71.561327),('cerroColorado',-16.363396,-71.561713),('cerroColorado',-16.361825,-71.562084),('cerroColorado',-16.360739,-71.562391),('cerroColorado',-16.360703,-71.564834)
) AS p(ruta, lat, lon);

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

-- ── Viajes finalizados (entre 6:00 y 22:00 hora de Perú; se guardan en UTC = Perú + 5 h) ──
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
       pt.lat, pt.lon,
       vj.fecha_inicio + make_interval(secs => random() * vj.duracion_minutos * 60)
FROM viajes vj
JOIN usuarios u  ON u.id = vj.usuario_id
JOIN demo_conductores d ON d.correo = u.correo
JOIN camaras c   ON c.vehiculo_id = vj.vehiculo_id
CROSS JOIN LATERAL generate_series(1, 4 + floor(random() * 12)::int + vj.id % 1) e
CROSS JOIN LATERAL (
    SELECT p.lat, p.lon FROM demo_puntos p
    WHERE p.ruta = ANY (d.rutas) AND e > 0
    ORDER BY random() LIMIT 1
) pt;

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
