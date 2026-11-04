-- Placas de la flota demo: todas con el formato ABC-123 (como V6C-271),
-- con letras variadas. Solo cambia la placa; viajes, puntos y pagos no se tocan.
UPDATE vehiculos v
   SET placa = n.placa
  FROM (VALUES ('900000001', 'AKR-482'),   -- Carla Quispe Mamani
               ('900000002', 'C4T-719'),   -- Diego Ramos Flores
               ('900000003', 'D8R-415'),   -- Victoriano Cahua Huamani
               ('900000004', 'F5H-732'),   -- Marco Huamán Ccori
               ('900000005', 'H7F-305'),   -- Sofía Mendoza Ríos
               ('900000006', 'B2J-609'),   -- Jorge Paredes León
               ('900000007', 'AWN-157'),   -- Rosa Chávez Gutiérrez
               ('900000008', 'T9L-864'),   -- Pedro Salas Núñez
               ('900000009', 'CTZ-640'),   -- Ana Vargas Soto
               ('900000010', 'M2K-938'),   -- Luis Fernández Paz
               ('900000011', 'P7D-554'),   -- Milagros Rojas Díaz
               ('900000012', 'X4N-812'),   -- Kevin Castillo Lázaro
               ('900000013', 'V6C-271')    -- Valeria Gómez Paucar
       ) AS n(telefono, placa)
  JOIN usuarios u ON u.telefono = n.telefono
 WHERE v.usuario_id = u.id;

SELECT u.nombres || ' ' || u.apellidos AS conductor, v.placa, v.tipo_vehiculo
  FROM usuarios u JOIN vehiculos v ON v.usuario_id = u.id
 WHERE u.telefono LIKE '9000000__'
 ORDER BY u.telefono;
