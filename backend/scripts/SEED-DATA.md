# Demo data seeding

Loads a presentation account with sample applications, resumes, and AI analysis records.

## Credentials

| Field    | Value                 |
|----------|-----------------------|
| Email    | `demo@jobtracker.app` |
| Password | `Demo123!`            |

## Prerequisites

- Database schema must exist (start the API once, or run `./scripts/init-db.sh`).
- For Docker-only Postgres (production compose), Docker must be running.

## Run

From `backend/`:

```bash
chmod +x scripts/seed-demo-data.sh   # once
./scripts/seed-demo-data.sh
```

Replace existing demo data:

```bash
./scripts/seed-demo-data.sh --force
```

## Connection string

The script picks a connection automatically:

1. `ConnectionStrings__DefaultConnection` if already set in the environment
2. Docker network `Host=postgres` when `jobtracker_postgres` runs without a published port (typical deploy)
3. `Host=localhost` using `POSTGRES_*` from `.env` otherwise

Override manually:

```bash
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=JobTrackerDb;Username=jobtracker;Password=your_password'
./scripts/seed-demo-data.sh
```

Or:

```bash
dotnet run --project JobTracker.Seed -- --connection "Host=...;Port=5432;Database=...;Username=...;Password=..."
```

## Deployed server (Linode)

SSH to the server, `cd` to the backend directory (same path as deploy), ensure `.env` matches production, then:

```bash
./scripts/seed-demo-data.sh
```

If the stack is up, the script uses the internal Docker network. No need to expose Postgres on the host.

## What gets created

- 1 demo user (BCrypt password, same as production auth)
- 2 resumes (metadata only; PDF files are not uploaded to disk)
- 15 job applications across all statuses (for sorting/filter demos)
- 4 completed AI analysis jobs on applications with resumes
