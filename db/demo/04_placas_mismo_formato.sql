-- Placas de la flota demo: todas con el formato ABC-123 (como V6C-271),
-- con letras variadas. Solo cambia la placa; viajes, puntos y pagos no se tocan.
UPDATE vehiculos v
   SET placa = n.placa
  FROM (VALUES ('976412358', 'AKR-482'),   -- Carla Quispe Mamani
               ('951287604', 'C4T-719'),   -- Diego Ramos Flores
               ('989486625', 'D8R-415'),   -- Victoriano Cahua Huamani
               ('943170582', 'F5H-732'),   -- Marco Huamán Ccori
               ('962835147', 'H7F-305'),   -- Sofía Mendoza Ríos
               ('917564230', 'B2J-609'),   -- Jorge Paredes León
               ('984203716', 'AWN-157'),   -- Rosa Chávez Gutiérrez
               ('935619842', 'T9L-864'),   -- Pedro Salas Núñez
               ('958741063', 'CTZ-640'),   -- Ana Vargas Soto
               ('921398475', 'M2K-938'),   -- Luis Fernández Paz
               ('967052381', 'P7D-554'),   -- Milagros Rojas Díaz
               ('944826917', 'X4N-812'),   -- Kevin Castillo Lázaro
               ('973519046', 'V6C-271')    -- Valeria Gómez Paucar
       ) AS n(telefono, placa)
  JOIN usuarios u ON u.telefono = n.telefono
 WHERE v.usuario_id = u.id;

SELECT u.nombres || ' ' || u.apellidos AS conductor, v.placa, v.tipo_vehiculo
  FROM usuarios u JOIN vehiculos v ON v.usuario_id = u.id
 WHERE u.password_hash = '$2a$12$vYIauyQmZVRn7hZA9qKMte159Z2a/67htDmAf0.YDKsb/Tus16vMS'
 ORDER BY u.telefono;
