-- Cambia los teléfonos 9000000xx de las cuentas demo por números de celular variados.
-- Solo cambia el teléfono: viajes, puntos, placas y pagos no se tocan.
UPDATE usuarios u
   SET telefono = n.nuevo
  FROM (VALUES
               ('900000001', '976412358'),   -- Carla Quispe Mamani
               ('900000002', '951287604'),   -- Diego Ramos Flores
               ('900000003', '989486625'),   -- Victoriano Cahua Huamani
               ('900000004', '943170582'),   -- Marco Huamán Ccori
               ('900000005', '962835147'),   -- Sofía Mendoza Ríos
               ('900000006', '917564230'),   -- Jorge Paredes León
               ('900000007', '984203716'),   -- Rosa Chávez Gutiérrez
               ('900000008', '935619842'),   -- Pedro Salas Núñez
               ('900000009', '958741063'),   -- Ana Vargas Soto
               ('900000010', '921398475'),   -- Luis Fernández Paz
               ('900000011', '967052381'),   -- Milagros Rojas Díaz
               ('900000012', '944826917'),   -- Kevin Castillo Lázaro
               ('900000013', '973519046'),   -- Valeria Gómez Paucar
               ('900000014', '939786268'),   -- Wilder Salas Mayhua
               ('900000015', '959375112'),   -- Omar André Huamani
               ('900000016', '910634551'),   -- Jorge Steven Nina Leandres
               ('900000017', '958108821'),   -- Mauricio J. Rodríguez H.
               ('900000018', '924515874')    -- Esteban Blas Aguilar
       ) AS n(anterior, nuevo)
 WHERE u.telefono = n.anterior;

SELECT nombres || ' ' || apellidos AS conductor, telefono, correo
  FROM usuarios
 WHERE password_hash = '$2a$12$vYIauyQmZVRn7hZA9qKMte159Z2a/67htDmAf0.YDKsb/Tus16vMS'
 ORDER BY nombres;
