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

## Agentes remotos

Además del monitoreo desde la oficina (que corre en el mismo proceso del backend, como agente
built-in "Oficina"), se pueden sumar **agentes en otras sedes/ISPs** que traza los mismos
`Target`s y reportan sus corridas al backend central por HTTP — mismo patrón "el agente llama
hacia afuera" que el runner de deploy, así no hace falta abrir nada entrante en la sede remota.

1. Dar de alta el agente en el backend central (guarda solo el hash de la clave — copiarla, no se
   vuelve a mostrar):

   ```bash
   curl -X POST https://<host-central>/api/agents \
     -H "Content-Type: application/json" \
     -d '{"name":"Sucursal X","location":"Ciudad, Provincia","provider":"ISP local"}'
   ```

2. En la sede remota:

   ```bash
   git clone https://github.com/SwitchIT-AR/trace-monitor.git /opt/trace-monitor-agent
   cd /opt/trace-monitor-agent
   cp .env.agent.example .env   # completar CENTRAL_API_BASE_URL y AGENT_API_KEY
   docker compose -f docker-compose.agent.yml up -d --build
   ```

   El agente necesita sockets ICMP raw igual que el backend (`cap_add: NET_RAW, NET_ADMIN`); si
   la sede es un CT unprivileged de Proxmox, aplica la misma nota de `nesting=1,keyctl=1` de más
   arriba.

3. Verificar en `GET /api/agents` que el agente aparece y que `lastSeenAtUtc` se actualiza cada
   ciclo.

Los cambios de ruta se detectan por separado para cada par (target, agente): dos agentes en
ubicaciones distintas pueden ver rutas distintas y estables hacia el mismo destino sin que eso
se marque como un cambio.

## Deploy automático (GitHub Actions)

El CT está en la LAN de la oficina (`10.0.93.113`), sin IP pública — un
runner de GitHub Actions hosteado por GitHub no puede entrar por SSH desde
afuera. Por eso el deploy corre con un **runner self-hosted instalado en el
mismo CT**: el runner es el que llama hacia afuera a GitHub a buscar trabajo,
no al revés, así que no hace falta abrir nada entrante.

Cada push a `main` corre `.github/workflows/deploy.yml`:
1. `build-check` (dotnet + npm) en un runner de GitHub normal, como gate de
   sanidad.
2. `deploy`, en el runner self-hosted (`runs-on: [self-hosted, trace-monitor]`),
   hace `git reset --hard origin/main && docker compose up -d --build`
   directo en `/opt/trace-monitor`.

Setup del runner en el CT (una sola vez):

```bash
useradd -m -s /bin/bash ghrunner
usermod -aG docker ghrunner
mkdir -p /opt/actions-runner && chown ghrunner:ghrunner /opt/actions-runner
su - ghrunner -c '
  cd /opt/actions-runner
  curl -o runner.tar.gz -L https://github.com/actions/runner/releases/latest/download/actions-runner-linux-x64-<version>.tar.gz
  tar xzf runner.tar.gz
  ./config.sh --url https://github.com/SwitchIT-AR/trace-monitor --token <token-de-Settings-Actions-Runners> --labels self-hosted,linux,x64,trace-monitor
'
cd /opt/actions-runner && ./svc.sh install ghrunner && ./svc.sh start
```

El token de registro sale de Settings → Actions → Runners → New self-hosted
runner (o `gh api -X POST repos/OWNER/REPO/actions/runners/registration-token`).

En el CT, antes del primer deploy:

```bash
mkdir -p /opt/trace-monitor
git clone https://github.com/SwitchIT-AR/trace-monitor.git /opt/trace-monitor
cd /opt/trace-monitor
cp .env.example .env   # completar POSTGRES_PASSWORD
```
