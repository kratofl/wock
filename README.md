# Wock

Wock is a .NET 10 Blazor Web App using Interactive Server render mode with global interactivity.

## Local development

```powershell
dotnet restore Wock.sln
dotnet build Wock.sln
dotnet test Wock.sln
dotnet run --project src\Wock\Wock.csproj
```

The Development connection string uses `wock.dev.db` in the application working directory. The default non-development connection string uses `/data/wock.db`.

## Docker local deployment

Build and start Wock with Docker Compose:

```powershell
docker compose up -d --build
```

Open Wock at <http://localhost:8080>.

The Compose service is named `wock`, maps host port `8080` to container port `8080`, and restarts with `unless-stopped`. The container is configured with:

- `ASPNETCORE_URLS=http://+:8080`
- `ConnectionStrings__WockDb=Data Source=/data/wock.db`
- `Plugins__StoragePath=/plugins`

### Persistent data and backups

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

### Plugin storage and installation

Plugin installation uses the `Plugins:StoragePath` configuration key. In Docker, `Plugins__StoragePath` points to `/plugins`, so installed plugin assemblies persist in the `wock-plugins` volume. If you change the plugin storage location, mount a persistent volume at the same path and update `Plugins__StoragePath` accordingly.

### Start Wock when the PC starts

For normal local startup, enable Docker Desktop to start when Windows starts. The Compose service uses `restart: unless-stopped`, so Docker restarts Wock after Docker Desktop comes up unless you explicitly stopped the service.

Windows Task Scheduler can also start Docker Compose on login or boot, but that changes host startup behavior and should be approved separately before adding it.
