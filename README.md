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

#### Production Environment Variables

Configure the following environment variables for production deployment:

```
ConnectionStrings__Default=Host=...;Database=...;Username=...;Password=...
SeatsIoApi__SecretKey=<Seats.io workspace secret key>
SeatsIoApi__Region=<Seats.io region>
```
