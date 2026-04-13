# XBOL Ticketing API

API for the XBOL ticketing system (events, venues, tickets, orders).

## Development Setup

This refers to development using Visual Studio 2026 on Windows, or using the .NET 10 SDK through the command line.

### Requirements

- [Visual Studio 2026](https://visualstudio.microsoft.com/insiders/) (Windows)
- [.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) (Linux)
- PostgreSQL
- [Docker](https://www.docker.com/) or [Podman](https://podman.io/) (for development services)

### Quick Start

In Visual Studio, set **XBOL.Ticketing.API** as the Startup Project and press `F5`.

For the command-line interface:

```bash
make dev    # Start PostgreSQL
dotnet run --project XBOL.Ticketing/XBOL.Ticketing.API
```

API runs at <http://localhost:5103>.

### Build & Compilation

```bash
dotnet build XBOL.Ticketing/XBOL.Ticketing.sln
```

### Secrets

Configure secrets using .NET Secret Manager:

```bash
dotnet user-secrets set "SeatsIoApi:SecretKey" "YOUR_KEY" --project XBOL.Ticketing/XBOL.Ticketing.API
```

List configured secrets:

```bash
dotnet user-secrets list --project XBOL.Ticketing/XBOL.Ticketing.API
```

### Configuration

Edit `appsettings.Development.json` for local settings (connection strings, service URLs, etc.). Settings cascade: `appsettings.json` → `appsettings.{Environment}.json` → environment variables. All settings are validated at startup.

IDE autocomplete is provided by `appsettings.schema.json`, which regenerates automatically on Debug builds.

## Deployment

The container is production-ready with:

- **Security**: Non-root `app` user
- **Health checks**: Automatic container health monitoring
- **Restart policy**: `unless-stopped` for high availability
- **Environment**: `ASPNETCORE_ENVIRONMENT=Production`

#### Requirements

- Make
- [Podman](https://podman.io/) (or [Docker](https://www.docker.com/))
- [Podman Compose](https://docs.podman.io/en/latest/markdown/podman-compose.1.html) (or [Docker Compose](https://docs.docker.com/compose/))

#### Usage

```bash
make build    # Create the Docker container
make run      # Run the Docker Compose environment
```

**Access the containerized services**

- **API Base URL**: <http://localhost:5103>
- **API Health Check**: <http://localhost:5103/healthz>

#### GCP Secrets

Runtime configuration is stored in GCP Secret Manager. Each environment has a dedicated secret:

| Secret                          | Contents                                                                       |
| ------------------------------- | ------------------------------------------------------------------------------ |
| `dev-xbol-db-secret`            | PostgreSQL credentials (`DB_HOST`, `DB_PORT`, `DB_USER`, `DB_PASS`, `DB_NAME`) |
| `dev-xbol-api-ticketing-secret` | App configuration (connection strings, Seats.io credentials)                   |

The app secret stores environment variables using ASP.NET's `__` (double underscore) convention for nested config:

```json
{
    "ConnectionStrings__Default": "Host=<DB_HOST>;Port=<DB_PORT>;Database=<DB_NAME>;Username=<DB_USER>;Password=<DB_PASS>",
    "SeatsIoApi__SecretKey": "<SEATS_IO_SECRET_KEY>"
}
```

Connection strings are assembled from the values in `dev-xbol-db-secret`. To update:

```bash
gcloud secrets versions add dev-xbol-api-ticketing-secret --data-file=- <<'EOF'
{ ... }
EOF
```

QA secrets follow the same pattern with a `qa-` prefix.
