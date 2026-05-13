# Wock

Wock is a .NET 10 Blazor Web App using Interactive Server render mode with global interactivity. It uses MudBlazor for shared UI feedback components such as status and error messages.

## Prerequisites

- Git
- .NET 10 SDK
- Docker Desktop, optional for Docker Compose deployment, persistent volumes, or local infrastructure checks

## Local setup from scratch

Clone the repository with normal Git, then restore, build, test, and run the app. Use the SSH URL below, or the HTTPS URL from GitHub if you do not use SSH keys:

```powershell
git clone git@github.com:kratofl/wock.git
cd wock
dotnet restore Wock.sln
dotnet build Wock.sln
dotnet test Wock.sln
dotnet run --project src\Wock\Wock.csproj
```

Open the URL printed by `dotnet run`. The Development database is SQLite and uses `wock.dev.db` by default, relative to the process working directory. No database container is required for the normal local setup; EF Core applies migrations automatically on startup.

To start from a clean local SQLite database, stop the app and remove the generated database files:

```powershell
Remove-Item .\wock.dev.db* -ErrorAction SilentlyContinue
dotnet run --project src\Wock\Wock.csproj
```

The default non-development connection string uses `/data/wock.db`, which is the path used by the Docker image.

## Project structure

Wock is a modular monolith. The web project still lives at `src\Wock\Wock.csproj`, but domain, application abstractions, infrastructure, and EF Core migrations are split into separate projects:

| Project | Responsibility |
| --- | --- |
| `src\Wock.Domain` | Domain entities, audit base classes, feature-owned models, and the lightweight `ApplicationUser` audit/user model. |
| `src\Wock.Application` | Application-level abstractions such as current-user and clock contracts. |
| `src\Wock.Infrastructure` | EF Core `AppDbContext`, database provider selection, technical plugin install/load services, and infrastructure implementations. |
| `src\Wock.Migrations.Sqlite` | SQLite migration assembly. |
| `src\Wock.Migrations.SqlServer` | SQL Server migration assembly. |
| `src\Wock.Migrations.Postgres` | PostgreSQL migration assembly. |
| `src\Wock` | Blazor UI, `Program.cs`, appsettings, request logging, and DI composition. |
| `src\Wock.Abstractions` | Stable plugin/connector contracts for external plugin authors. |

Feature slices stay visible inside each layer, for example customers have their domain model in `Wock.Domain`, UI in `Wock`, and persistence configuration through `Wock.Infrastructure`.

## Database provider

Wock keeps the EF Core provider and connection string together in the `Database` configuration section. SQLite is the default for local development and Docker.

Supported provider values are `Sqlite`, `SqlServer` and `Postgres`. `MsSql`, `PostgreSQL`, `Npgsql` and `SqlLite` are accepted aliases.

```json
{
  "Database": {
    "Provider": "Sqlite",
    "ConnectionString": "Data Source=wock.dev.db"
  }
}
```

Use environment variables for non-local secrets, for example:

```powershell
$env:Database__Provider = "SqlServer"
$env:Database__ConnectionString = "Server=.;Database=Wock;Trusted_Connection=True;TrustServerCertificate=True"

$env:Database__Provider = "Postgres"
$env:Database__ConnectionString = "Host=localhost;Database=wock;Username=wock;Password=<secret>"
```

### EF Core migrations

Runtime migrations are selected by `Database:Provider`:

| Provider | Migration assembly |
| --- | --- |
| `Sqlite` | `Wock.Migrations.Sqlite` |
| `SqlServer` | `Wock.Migrations.SqlServer` |
| `Postgres` | `Wock.Migrations.Postgres` |

Add provider-specific migrations with the matching migrations project and the web project as startup:

```powershell
dotnet ef migrations add AddSomething --project src\Wock.Migrations.Sqlite\Wock.Migrations.Sqlite.csproj --startup-project src\Wock\Wock.csproj -- --provider Sqlite
dotnet ef migrations add AddSomething --project src\Wock.Migrations.SqlServer\Wock.Migrations.SqlServer.csproj --startup-project src\Wock\Wock.csproj -- --provider SqlServer
dotnet ef migrations add AddSomething --project src\Wock.Migrations.Postgres\Wock.Migrations.Postgres.csproj --startup-project src\Wock\Wock.csproj -- --provider Postgres
```

The existing SQLite migration IDs were preserved so existing SQLite databases can continue to migrate forward.

## Usage

Start the app locally, then open the URL printed by `dotnet run`. The main navigation includes:

- Time Tracking: start, pause, stop, and quickly switch between tasks.
- Reports: review tracked work.
- Customers: manage customer records.
- Tasks: manage booking targets/tasks for tracked work.
- Plugins: install, enable, disable, and inspect plugins.

## Structured logging

Wock writes structured JSON logs to stdout/stderr with Serilog's compact JSON formatter. This is intended for container log collection by Graylog and dashboarding in Grafana; configure Graylog or your collector to parse the Docker log message as JSON.

Every HTTP request runs inside a logging scope with:

- `RequestId`: uses the incoming `X-Request-ID` header when present, otherwise the ASP.NET Core request trace identifier. The value is echoed back in the `X-Request-ID` response header.
- `TraceId`: uses the current W3C `Activity` trace ID when available, otherwise falls back to the request trace identifier.
- `UserId`: uses `ICurrentUserContext.UserId`; anonymous requests keep this value empty.

Application-wide log events also include `Application` and `Environment`. Log levels are configured in the `Serilog:MinimumLevel` section and can be overridden with environment variables such as `Serilog__MinimumLevel__Default=Debug`.

## Plugin installation

Open the Plugins page at `/plugins`. Install a plugin from either:

- A plugin folder path containing `wock-plugin.json`.
- A plugin ZIP path containing `wock-plugin.json` and no path traversal entries.

Plugin installation uses the `Plugins:StoragePath` configuration key. In Docker, `Plugins__StoragePath` points to `/plugins`, so installed plugin assemblies persist in the `wock-plugins` volume. If you change the plugin storage location, mount a persistent volume at the same path and update `Plugins__StoragePath` accordingly.

## Docker Compose local run

The included Compose file builds and runs Wock with SQLite stored in a named Docker volume. This is useful when you want a container-like local run without installing or configuring a separate database service:

```powershell
docker compose up -d --build
```

Open Wock at <http://localhost:8080>.

The Compose service is named `wock`, maps host port `8080` to container port `8080`, and restarts with `unless-stopped`. The container is configured with:

- `ASPNETCORE_URLS=http://+:8080`
- `Database__Provider=Sqlite`
- `Database__ConnectionString=Data Source=/data/wock.db`
- `Plugins__StoragePath=/plugins`

Validate the Docker deployment with:

```powershell
docker compose up -d --build
curl http://localhost:8080
docker compose logs -f wock
docker compose down
```

`docker compose logs -f wock` shows the same structured JSON logs that a Docker log collector can forward to Graylog.

### Optional database infrastructure

SQLite is enough for normal development. If you want to validate another provider locally, run that database in a separate Compose stack and point Wock at it with environment variables. For example, save this as a local, uncommitted `docker-compose.postgres.yml`:

```yaml
services:
  postgres:
    image: postgres:17
    environment:
      POSTGRES_DB: wock
      POSTGRES_USER: wock
      POSTGRES_PASSWORD: wock
    ports:
      - "5432:5432"
    volumes:
      - wock-postgres:/var/lib/postgresql/data

volumes:
  wock-postgres:
```

Start PostgreSQL, then run Wock against it:

```powershell
docker compose -f docker-compose.postgres.yml up -d
$env:Database__Provider = "Postgres"
$env:Database__ConnectionString = "Host=localhost;Port=5432;Database=wock;Username=wock;Password=wock"
dotnet run --project src\Wock\Wock.csproj
```

### Optional logging infrastructure

Graylog/Grafana are not required to run Wock locally. For local verification, read the JSON log lines from the console or from `docker compose logs -f wock`. If you run a local Graylog stack, keep it as separate infrastructure and configure Graylog or your collector to parse the Wock container logs as JSON; no Graylog endpoint or credentials are required in this repository.

## Production and self-hosted deployment

Wock is intended to be self-hosted by each user or small team. The recommended production shape is a single Wock container behind a reverse proxy that handles HTTPS, with persistent storage mounted for the database and installed plugins.

```text
Internet/LAN
  -> Reverse proxy with TLS, for example Caddy, Traefik, or nginx
  -> Wock container on http://wock:8080
  -> Persistent database and plugin storage
  -> Docker/container logs collected by Graylog, Grafana Alloy, Promtail, or another collector
```

For a small personal installation, SQLite in a persistent Docker volume is the simplest production setup. For multi-user installations or hosts where you already run database infrastructure, use PostgreSQL or SQL Server and configure Wock with environment variables.

### Minimal production Compose file

Use this as a starting point for a self-hosted deployment. Keep local changes and secrets outside the repository, for example in an uncommitted or ignored `docker-compose.prod.yml` and `.env` file.

```yaml
services:
  wock:
    build:
      context: .
      dockerfile: Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
      ASPNETCORE_FORWARDEDHEADERS_ENABLED: "true"
      AllowedHosts: wock.example.com
      Database__Provider: Sqlite
      Database__ConnectionString: Data Source=/data/wock.db
      Plugins__StoragePath: /plugins
      Serilog__MinimumLevel__Default: Information
    volumes:
      - wock-data:/data
      - wock-plugins:/plugins
    restart: unless-stopped
    networks:
      - proxy

volumes:
  wock-data:
  wock-plugins:

networks:
  proxy:
    external: true
```

Start or update the app with:

```powershell
docker network create proxy
docker compose -f docker-compose.prod.yml up -d --build
docker compose -f docker-compose.prod.yml logs -f wock
```

Run `docker network create proxy` only once; if the network already exists, continue with the Compose command. Expose Wock through your reverse proxy and terminate TLS there. The Wock container should usually stay on the private Docker network instead of being published directly to the internet. If your reverse proxy runs on the host instead of in Docker, publish only to loopback with `ports: ["127.0.0.1:8080:8080"]` and point the proxy at `http://127.0.0.1:8080`.

If your reverse proxy forwards `X-Forwarded-For` and `X-Forwarded-Proto`, keep `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` so generated URLs, HTTPS redirection, and request metadata use the public scheme and client address.

### Production database options

SQLite is acceptable for a lightweight self-hosted instance when the `/data` volume is backed up. For PostgreSQL, run the database as separate infrastructure and set:

```powershell
$env:Database__Provider = "Postgres"
$env:Database__ConnectionString = "Host=<db-host>;Port=5432;Database=wock;Username=<user>;Password=<secret>"
```

For SQL Server, set:

```powershell
$env:Database__Provider = "SqlServer"
$env:Database__ConnectionString = "Server=<db-host>;Database=Wock;User Id=<user>;Password=<secret>;TrustServerCertificate=True"
```

Do not commit production connection strings, passwords, TLS certificates, or Graylog credentials.

### Production logging

Wock emits structured JSON logs to stdout/stderr. In production, let Docker or your container runtime collect those logs and forward them to Graylog. Grafana can then query the log backend configured for your environment. The application does not need a direct Graylog endpoint for the default deployment.

Relevant fields for correlation and filtering include `Application`, `Environment`, `RequestId`, `TraceId`, `UserId`, `RequestMethod`, `RequestPath`, `StatusCode`, and `Elapsed`.

### Updates and backups

Before updating the container image or rebuilding from a newer checkout, back up the database and plugin volumes. For SQLite-based deployments, the critical files are in the `wock-data` and `wock-plugins` volumes. Stop Wock before copying the SQLite database, or use a database-aware backup procedure.

```powershell
docker compose -f docker-compose.prod.yml down
docker run --rm -v wock_wock-data:/data -v ${PWD}:/backup alpine tar czf /backup/wock-data-backup.tar.gz -C /data .
docker run --rm -v wock_wock-plugins:/plugins -v ${PWD}:/backup alpine tar czf /backup/wock-plugins-backup.tar.gz -C /plugins .
docker compose -f docker-compose.prod.yml up -d --build
```

For PostgreSQL or SQL Server deployments, use the database server's normal backup tooling instead of copying container files.

## Persistent data and backups

Compose creates two named volumes:

- `wock-data` mounted at `/data`, containing the SQLite database at `/data/wock.db`.
- `wock-plugins` mounted at `/plugins`, containing installed plugin packages.

Back up these named volumes before rebuilding hosts or deleting Docker data. On Docker Desktop, named volumes are managed by Docker; inspect them with:

```powershell
docker volume inspect wock_wock-data
docker volume inspect wock_wock-plugins
```

Stop the app without deleting volumes:

```powershell
docker compose down
```

Only use `docker compose down --volumes` when you intentionally want to delete the database and plugin storage.

## GitHub repository

The repository is hosted on GitHub:

- SSH: `git@github.com:kratofl/wock.git`
- Web: <https://github.com/kratofl/wock>
- Default branch: `main`

## License

Wock is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## Start Wock when the PC starts

For normal local startup, enable Docker Desktop to start when Windows starts. The Compose service uses `restart: unless-stopped`, so Docker restarts Wock after Docker Desktop comes up unless you explicitly stopped the service.

Windows Task Scheduler can also start Docker Compose on login or boot, but that changes host startup behavior and should be approved separately before adding it.
