# XBOL Ticketing API

## Requirements

- .NET 10 SDK
- Docker (for PostgreSQL)

## Secrets

Configure secrets using .NET Secret Manager:

```bash
dotnet user-secrets set "SeatsIoApi:SecretKey" "YOUR_KEY" --project XBOL.Ticketing/XBOL.Ticketing.API
```

List configured secrets:

```bash
dotnet user-secrets list --project XBOL.Ticketing/XBOL.Ticketing.API
```

## Quickstart

```bash
docker compose -f docker-compose.dev.yml up -d
dotnet run --project XBOL.Ticketing/XBOL.Ticketing.API
```

API runs at <http://localhost:5103> (or <https://localhost:7021>).

## Database

```bash
# Start
docker compose -f docker-compose.dev.yml up -d

# Stop (keeps data)
docker compose -f docker-compose.dev.yml down

# Stop and delete data
docker compose -f docker-compose.dev.yml down -v
```

### Logs

```bash
# View logs
docker compose -f docker-compose.dev.yml logs postgres

# Follow logs
docker compose -f docker-compose.dev.yml logs -f postgres
```

### Postgres Tools

```bash
# Connect to psql
docker compose -f docker-compose.dev.yml exec postgres psql -U postgres -d XBOL

# Run a SQL file
docker compose -f docker-compose.dev.yml exec -T postgres psql -U postgres -d XBOL < file.sql

# Backup
docker compose -f docker-compose.dev.yml exec -T postgres pg_dump -U postgres XBOL > backup.sql

# Restore
docker compose -f docker-compose.dev.yml exec -T postgres psql -U postgres -d XBOL < backup.sql
```

### Migrations

Run from `XBOL.Ticketing/XBOL.Ticketing.Data`:

```bash
dotnet ef migrations add <MigrationName> --startup-project ../XBOL.Ticketing.API
dotnet ef database update --startup-project ../XBOL.Ticketing.API
```

### Seed Data

```bash
docker compose -f docker-compose.dev.yml exec -T postgres psql -U postgres -d XBOL < seed.sql
```
