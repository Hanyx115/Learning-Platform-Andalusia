# Local Development Guide

This guide explains both supported local workflows:

- Recommended: SQL Server and the ASP.NET Core API in Docker Compose.
- Debugging: SQL Server in Docker with the API started from Visual Studio.
- In both workflows, the React frontend runs through Vite on Windows.

Docker Compose does not currently run the frontend.

## 1. Prerequisites

Install:

- Docker Desktop with Linux containers enabled.
- .NET 10 SDK.
- Node.js `20.19+` or `22.12+`.
- Git.
- SQL Server Management Studio or another SQL client when direct database access is needed.

Run all commands from the repository root unless a step says otherwise.

## 2. Create the local Docker configuration

Create the ignored root environment file:

```powershell
Copy-Item .env.example .env
```

The supplied development values use:

```text
API_HTTP_PORT=8080
SQLSERVER_PORT=1433
```

`MSSQL_SA_PASSWORD` is the local SQL Server administrator password. Keep `.env` local and never commit it.

## 3. Choose one backend workflow

Use only one API workflow at a time:

- Use **Docker Compose** for the simplest complete setup.
- Use **Visual Studio** when debugging backend code with breakpoints.

Running both API instances is possible because they use different ports, but it is easy to test the wrong instance. Prefer one active API during normal development.

## 4. Workflow A: run SQL Server and the API with Docker Compose

Make sure Docker Desktop is running, then build and start both containers:

```powershell
docker compose up --build -d
```

Check their state:

```powershell
docker compose ps
```

Wait until the SQL Server container reports `healthy`. The API container should report `running`.

Useful backend URLs:

| Service | URL |
|---|---|
| Swagger UI | `http://localhost:8080/swagger` |
| OpenAPI document | `http://localhost:8080/openapi/v1.json` |
| Health check | `http://localhost:8080/health` |
| Versioned ping | `http://localhost:8080/api/v1/ping` |

If `API_HTTP_PORT` is changed in `.env`, replace `8080` in these URLs with the configured port.

## 5. Apply the database migrations

The containers start SQL Server and the API, but the API does not apply EF Core migrations automatically. On the first setup, and whenever a new migration is pulled, run:

```powershell
dotnet tool restore

$sqlPasswordLine = Get-Content .env |
    Where-Object { $_ -like 'MSSQL_SA_PASSWORD=*' } |
    Select-Object -First 1
$sqlPassword = ($sqlPasswordLine -split '=', 2)[1].Trim()

$sqlPortLine = Get-Content .env |
    Where-Object { $_ -like 'SQLSERVER_PORT=*' } |
    Select-Object -First 1
$sqlPort = if ($sqlPortLine) {
    ($sqlPortLine -split '=', 2)[1].Trim()
} else {
    '1433'
}

$env:ConnectionStrings__DefaultConnection = "Server=localhost,$sqlPort;Database=AndalusiaAcademy;User Id=sa;Password=$sqlPassword;TrustServerCertificate=True"

dotnet ef database update `
    --project src\AndalusiaAcademy.Infrastructure `
    --startup-project src\AndalusiaAcademy.API

Remove-Item Env:ConnectionStrings__DefaultConnection
```

The command reads `SQLSERVER_PORT` from `.env` and defaults to `1433` when it is not set.

## 6. Load the development catalog demo data

The Development API adds any missing catalog demo records automatically after the database migrations are current. The seed includes realistic categories, courses, programs, career paths and their relationships. It is repeatable, preserves existing records and does not run outside Development.

After applying migrations in the Docker workflow, restart the API container:

```powershell
docker compose restart api
```

For the Visual Studio workflow, stop and restart the API. To disable automatic demo seeding for a local run, set `CatalogSeed__Enabled=false` in that process environment.

## 7. Workflow B: debug the backend from Visual Studio

Start only SQL Server:

```powershell
docker compose stop api
docker compose up -d sqlserver
```

Read the local password and port without printing the password:

```powershell
$sqlPasswordLine = Get-Content .env |
    Where-Object { $_ -like 'MSSQL_SA_PASSWORD=*' } |
    Select-Object -First 1
$sqlPassword = ($sqlPasswordLine -split '=', 2)[1].Trim()

$sqlPortLine = Get-Content .env |
    Where-Object { $_ -like 'SQLSERVER_PORT=*' } |
    Select-Object -First 1
$sqlPort = if ($sqlPortLine) {
    ($sqlPortLine -split '=', 2)[1].Trim()
} else {
    '1433'
}
```

For the Visual Studio `https` profile, save this local user secret:

```powershell
$connectionString = "Server=localhost,$sqlPort;Database=AndalusiaAcademy;User Id=sa;Password=$sqlPassword;TrustServerCertificate=True"

dotnet user-secrets set `
    --project src\AndalusiaAcademy.API\AndalusiaAcademy.API.csproj `
    'ConnectionStrings:DefaultConnection' `
    $connectionString
```

Then:

1. Open `AndalusiaAcademy.sln`.
2. Set `AndalusiaAcademy.API` as the startup project.
3. Select the `https` launch profile.
4. Press `F5` or `Ctrl+F5`.

For the Visual Studio `Container (Dockerfile)` profile, the API is inside a container, so use the Docker host name instead:

```powershell
$connectionString = "Server=host.docker.internal,$sqlPort;Database=AndalusiaAcademy;User Id=sa;Password=$sqlPassword;TrustServerCertificate=True"

dotnet user-secrets set `
    --project src\AndalusiaAcademy.API\AndalusiaAcademy.API.csproj `
    'ConnectionStrings:DefaultConnection' `
    $connectionString
```

Stop and restart debugging after changing the secret. The `https` and `Container (Dockerfile)` profiles use the same secret key, so set the matching value when switching between them. User secrets stay outside Git and must never be replaced with a committed password.

Visual Studio URLs are:

| Profile | Swagger UI |
|---|---|
| `https` | `https://localhost:7115/swagger` |
| `Container (Dockerfile)` | `https://localhost:7115/swagger` or `http://localhost:5032/swagger` |

## 8. Run the frontend

Open a second PowerShell terminal at the repository root:

```powershell
Set-Location frontend
Copy-Item .env.example .env.local
npm ci
npm run dev
```

When using Docker Compose, set this value in `frontend/.env.local`:

```text
VITE_API_BASE_URL=http://localhost:8080
```

When using Visual Studio's `https` profile, keep:

```text
VITE_API_BASE_URL=https://localhost:7115
```

Open the frontend at:

```text
http://localhost:5173
```

If the Docker API port is changed, update `VITE_API_BASE_URL` in `frontend/.env.local`, stop Vite with `Ctrl+C`, and run `npm run dev` again.

## 9. Connect to the database with SSMS

Use these connection settings:

| Setting | Value |
|---|---|
| Server name | `localhost,1433` by default, or `localhost,<SQLSERVER_PORT>` |
| Authentication | SQL Server Authentication |
| User name | `sa` |
| Password | The `MSSQL_SA_PASSWORD` value from the root `.env` file |
| Database | `AndalusiaAcademy` |
| Trust server certificate | Enabled |

After connecting, expand:

```text
Databases
└── AndalusiaAcademy
    └── Tables
```

Refresh the `Databases` or `Tables` node if the migrated objects do not appear immediately.

## 10. Daily startup

When the containers have already been built and the database has already been migrated:

```powershell
docker compose up -d
```

In another terminal:

```powershell
Set-Location frontend
npm run dev
```

Rebuild the API image after backend code or package changes:

```powershell
docker compose up --build -d
```

For the Visual Studio workflow, start only SQL Server and then launch the selected API profile:

```powershell
docker compose stop api
docker compose up -d sqlserver
```

## 11. Logs and troubleshooting

Show container status:

```powershell
docker compose ps
```

Follow API logs:

```powershell
docker compose logs -f api
```

Follow SQL Server logs:

```powershell
docker compose logs -f sqlserver
```

If a port is already in use, change `API_HTTP_PORT` or `SQLSERVER_PORT` in `.env`, then recreate the containers:

```powershell
docker compose down
docker compose up --build -d
```

Remember to update the frontend API URL, migration connection string, and SSMS server port when the corresponding Docker port changes.

If Visual Studio reports that `DefaultConnection` is required, the selected launch profile cannot see a database user secret. Repeat the matching command in section 7, stop debugging completely, and start the profile again.

If `localhost,1433` refuses the SQL connection:

```powershell
docker compose ps
Test-NetConnection localhost -Port 1433
```

Confirm that the SQL Server service is healthy and that the tested port matches `SQLSERVER_PORT` in `.env`.

## 12. Stop the project

Stop Vite with `Ctrl+C`, then stop and remove the Docker containers and network:

```powershell
docker compose down
```

The `sqlserver-data` Docker volume remains, so database data is preserved.

To completely reset the development database, including all local data, use:

```powershell
docker compose down -v
```

This last command permanently deletes the local Docker database volume. Start the containers and apply migrations again after a reset.

## 13. Verify the setup

For the Docker Compose workflow, the setup is ready when:

1. `docker compose ps` reports SQL Server as healthy and the API as running.
2. `http://localhost:8080/health` returns a successful response.
3. Swagger opens at `http://localhost:8080/swagger`.
4. The frontend opens at `http://localhost:5173`.
5. SSMS shows the `AndalusiaAcademy` database and its migrated tables.

For the Visual Studio workflow, `docker compose ps` only needs to show SQL Server as healthy. Verify the API through the URL for the selected profile in section 7.
