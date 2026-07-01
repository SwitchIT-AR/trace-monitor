# Trace Monitor

Monitoreo continuo del camino de red (MTR) desde la oficina hacia los ISP/DC
de Vaclog, Griveo y Managio. Corre `mtr` en ráfagas de reporte cada 2 minutos
por destino, guarda cada corrida, detecta automáticamente cuando cambia la
ruta, y lo muestra en un dashboard con mapa geográfico de los saltos.

## Stack

- **backend/** — ASP.NET Core (.NET 10) + EF Core/Npgsql + `BackgroundService`
  que dispara `mtr --json` por destino.
- **frontend/** — Vite + React + TypeScript + Mantine (+ `@mantine/charts`,
  `react-leaflet`).
- **Postgres** para el historial.
- **Docker Compose** para levantar todo junto.

Destinos monitoreados por defecto (seed en la migración inicial):

| Nombre        | Proveedor  | Host            |
|---------------|------------|-----------------|
| Griveo        | Telecentro | 186.19.218.8    |
| Vaclog        | IPLAN      | 190.210.245.120 |
| Managio (DC)  | Telecentro | 186.23.255.158  |
| Vaclog (DC)   | Telecentro | 186.23.255.156  |

Se pueden agregar más con `POST /api/targets`.

## Correr todo con Docker

```bash
cp .env.example .env   # y completar POSTGRES_PASSWORD
docker compose up -d --build
```

Dashboard en `http://<host>:8080` (puerto configurable con `FRONTEND_PORT`).

`mtr` necesita sockets ICMP raw — el contenedor `backend` corre con
`cap_add: [NET_RAW, NET_ADMIN]`. Si el host es un **CT unprivileged de
Proxmox**, puede que haga falta habilitar `features: nesting=1,keyctl=1` en
la config del CT antes de que esto funcione. Se puede verificar con:

```bash
docker run --rm --cap-add=NET_RAW --cap-add=NET_ADMIN alpine ping -c1 8.8.8.8
```

## Desarrollo local (sin Docker)

Backend:

```bash
cd backend
dotnet run --project src/TraceMonitor.Api
```

Necesita un Postgres accesible (connection string en
`src/TraceMonitor.Api/appsettings.json` o variable de entorno
`ConnectionStrings__TraceMonitor`). Las migraciones corren solas al arrancar.

Frontend:

```bash
cd frontend
npm install
npm run dev
```

El dev server de Vite proxya `/api` hacia `http://localhost:8080` (configurable
con `VITE_API_PROXY_TARGET`).

## Deploy automático (GitHub Actions)

Cada push a `main` corre `.github/workflows/deploy.yml`: build de sanity
(dotnet + npm) y después un deploy por SSH al CT que hace
`git reset --hard origin/main && docker compose up -d --build` en
`/opt/trace-monitor`.

Requiere estos secrets en el repo (Settings → Secrets and variables →
Actions):

- `DEPLOY_HOST` — IP del CT.
- `DEPLOY_USER` — usuario SSH (ej. `root`).
- `DEPLOY_SSH_KEY` — clave privada SSH dedicada al deploy.

Se recomienda generar un par de llaves solo para esto en vez de usar una
contraseña:

```bash
ssh-keygen -t ed25519 -f deploy_key -N ""
ssh-copy-id -i deploy_key.pub usuario@<CT_IP>
```

Cargar `deploy_key` (privada) como el secret `DEPLOY_SSH_KEY`.

En el CT, antes del primer deploy:

```bash
mkdir -p /opt/trace-monitor
git clone https://github.com/SwitchIT-AR/trace-monitor.git /opt/trace-monitor
cd /opt/trace-monitor
cp .env.example .env   # completar POSTGRES_PASSWORD
```
