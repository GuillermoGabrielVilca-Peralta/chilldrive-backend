-- Cambia a la conductora demo Lucía Torres Vega por Victoriano Cahua Huamani
-- (mismo usuario: conserva sus viajes, eventos, puntos y su pago Yape pendiente).
UPDATE usuarios
   SET nombres = 'Victoriano', apellidos = 'Cahua Huamani', correo = 'victorianocahua@gmail.com'
 WHERE telefono = '900000003';

UPDATE vehiculos v
   SET placa = '4821-VC'
  FROM usuarios u
 WHERE v.usuario_id = u.id AND u.telefono = '900000003';

SELECT u.id, u.nombres, u.apellidos, u.correo, v.placa
  FROM usuarios u JOIN vehiculos v ON v.usuario_id = u.id
 WHERE u.telefono = '900000003';
