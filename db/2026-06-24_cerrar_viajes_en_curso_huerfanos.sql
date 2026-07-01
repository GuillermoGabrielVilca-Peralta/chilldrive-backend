-- Limpieza puntual: cerrar los viajes que quedaron "En Curso" colgados.
-- Antes no existía el guard de "un solo viaje activo", así que un viaje iniciado
-- y nunca finalizado (app cerrada sin Finalizar, logout, reinstalación, etc.)
-- quedaba abierto para siempre. Esto los marca como Finalizados de una vez.
--
-- Correr UNA vez para dejar el historial limpio. De ahí en adelante el guard de
-- ViajeService.Iniciar evita que se vuelvan a acumular (retoma el de hoy y cierra
-- los de días anteriores automáticamente).
--
-- Nota: los puntos del conductor NO se tocan — ya se aplicaron evento por evento
-- en tiempo real; aquí solo se cierra el estado del viaje.
--
--   psql -h <host> -U <user> -d <db> -f db/2026-06-24_cerrar_viajes_en_curso_huerfanos.sql

UPDATE viajes
SET estado    = 'Finalizado',
    fecha_fin = COALESCE(fecha_fin, CURRENT_TIMESTAMP)
WHERE estado = 'En Curso';
