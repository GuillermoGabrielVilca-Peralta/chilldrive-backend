# db/init — scripts de inicialización de PostgreSQL

Los archivos `.sql` colocados aquí son ejecutados por el contenedor de PostgreSQL
**automáticamente la primera vez** que se crea el volumen `pgdata` (base de datos vacía).
Se ejecutan en orden alfabético, por eso se prefijan con números: `01_schema.sql`, `02_data.sql`, etc.

## Pasos para generar el script de esquema desde tu BD local

Desde la máquina que tiene la base de datos actual (requiere `pg_dump` instalado):

```powershell
# Solo el esquema (tablas, índices, secuencias, vistas, extensiones):
pg_dump -U postgres -d chilldrive --schema-only -f db/init/01_schema.sql

# Opcional — datos semilla (aliados, premios, etc.) para que el entorno nuevo ya tenga datos:
pg_dump -U postgres -d chilldrive --data-only --table=aliados --table=premios -f db/init/02_seed.sql
```

> Si `pg_dump` pide contraseña, agrega `-W` o configura `PGPASSWORD` en la terminal.

## Flujo completo de primer despliegue

```powershell
# 1. Copia el archivo de variables de entorno y rellena los valores reales
cp .env.example .env

# 2. Genera el schema (ver sección de arriba)

# 3. Levanta los contenedores
docker compose up -d

# 4. Verifica que la API responde
curl http://localhost:8080/health
```

## Notas

- Si el volumen `pgdata` ya existe (BD ya iniciada), los scripts de esta carpeta **no se vuelven a ejecutar**.  
  Para reiniciar desde cero: `docker compose down -v` (borra datos) → `docker compose up -d`.
- La extensión `pgcrypto` debe estar en el script de esquema (`CREATE EXTENSION IF NOT EXISTS pgcrypto;`) para que los QR de canjes se generen correctamente.
