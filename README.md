# Employee Management API

ASP.NET Core (.NET 10) Web API using Entity Framework Core and PostgreSQL.
Manages employees, departments, projects and skills, in layers:
`Controllers → Services → Repositories → AppDbContext`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (`dotnet --version`)
- PostgreSQL running locally (`pg_isready -h localhost -p 5432`)
- EF Core CLI tool: `dotnet tool install --global dotnet-ef`

## Setup

### 1. Create the database

```sql
-- sudo -u postgres psql -p 5432
CREATE USER efuser WITH PASSWORD '<choose-a-password>';
CREATE DATABASE employeemanagement OWNER efuser;
GRANT ALL PRIVILEGES ON DATABASE employeemanagement TO efuser;
```

The first migration runs `CREATE EXTENSION citext` (used for case-insensitive
emails). `citext` ships with PostgreSQL's contrib package; on Debian/Ubuntu
install `postgresql-contrib` if the migration reports it is missing.

### 2. Set the connection string

The connection string is **not stored in the repo**. For local development keep
it in [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
(stored outside the project, in `~/.microsoft/usersecrets/`):

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=employeemanagement;Username=efuser;Password=<your-password>"
```

Check it with `dotnet user-secrets list`.

User secrets are only loaded when `ASPNETCORE_ENVIRONMENT=Development`, which
`dotnet run` sets from `Properties/launchSettings.json`. In any other
environment, supply the value as an environment variable instead
(note the double underscore):

```bash
export ConnectionStrings__DefaultConnection="Host=...;Database=...;Username=...;Password=..."
```

If it is missing, the app stops at startup with a message saying so.

### 3. Create the tables

```bash
dotnet ef database update
```

### 4. Run

```bash
dotnet run
```

The app listens on `http://localhost:5085` (from `launchSettings.json`). If you
start it with `--urls`, that port wins instead.

## API documentation

In Development only:

- Swagger UI: `http://localhost:5085/swagger`
- OpenAPI document: `http://localhost:5085/openapi/v1.json`

`EF.http` contains ready-made requests (VS Code REST Client).

## Endpoints

| Resource | Routes |
|---|---|
| Departments | `GET`, `POST /api/departments` |
| Employees | `GET`, `POST /api/employees`; `GET`, `PUT`, `DELETE /api/employees/{id}` |
| Projects | `GET`, `POST /api/projects`; `GET /api/projects/{id}`; `POST /api/projects/{projectId}/employees`; `DELETE /api/projects/{projectId}/employees/{employeeId}` |
| Skills | `GET`, `POST /api/skills`; `GET /api/skills/{id}` |
| Employee skills | `GET`, `POST /api/employees/{employeeId}/skills`; `PUT`, `DELETE /api/employees/{employeeId}/skills/{skillId}` |

List endpoints (`GET` on departments, employees, projects, skills) are paged with
`?page=1&pageSize=20` (defaults 1 and 20, max page size 100, ordered by id) and return:

```json
{ "items": [ ... ], "page": 1, "pageSize": 20, "totalCount": 42, "totalPages": 3 }
```

Errors use RFC 7807 problem responses: `400` validation / bad reference,
`404` not found, `409` conflict (duplicate email, skill or assignment).

## Migrations

```bash
dotnet ef migrations add <Name>      # create
dotnet ef migrations list            # list, pending ones are marked
dotnet ef database update            # apply
dotnet ef migrations remove          # undo the latest (only if not applied)
```

More detail is in `DeveloperGuide.txt`.  It also documents the initial errors encountered during development, along with their respective resolutions and workarounds.