-- Unifica las placas de la flota demo al formato ABC-123 (como V6C-271).
-- Cambia solo las 5 motos/mototaxis que tenían el formato 1234-AB.
UPDATE vehiculos v
   SET placa = n.placa
  FROM (VALUES ('900000003', 'V8R-415'),   -- Victoriano Cahua Huamani
               ('900000004', 'V5H-732'),   -- Marco Huamán Ccori
               ('900000006', 'V2J-609'),   -- Jorge Paredes León
               ('900000011', 'V7P-554'),   -- Milagros Rojas Díaz
               ('900000012', 'V4N-812')    -- Kevin Castillo Lázaro
       ) AS n(telefono, placa)
  JOIN usuarios u ON u.telefono = n.telefono
 WHERE v.usuario_id = u.id;

SELECT u.nombres || ' ' || u.apellidos AS conductor, v.placa, v.tipo_vehiculo
  FROM usuarios u JOIN vehiculos v ON v.usuario_id = u.id
 WHERE u.telefono LIKE '9000000__'
 ORDER BY u.telefono;
