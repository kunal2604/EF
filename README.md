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
| Departments | `GET`, `POST /api/departments`; `GET`, `PUT`, `DELETE /api/departments/{id}` |
| Employees | `GET`, `POST /api/employees`; `GET`, `PUT`, `DELETE /api/employees/{id}` |
| Projects | `GET`, `POST /api/projects`; `GET`, `DELETE /api/projects/{id}`; `POST /api/projects/{projectId}/employees`; `DELETE /api/projects/{projectId}/employees/{employeeId}` |
| Skills | `GET`, `POST /api/skills`; `GET`, `DELETE /api/skills/{id}` |
| Employee skills | `GET`, `POST /api/employees/{employeeId}/skills`; `PUT`, `DELETE /api/employees/{employeeId}/skills/{skillId}` |

List endpoints (`GET` on departments, employees, projects, skills) are paged with
`?page=1&pageSize=20` (defaults 1 and 20, max page size 100, ordered by id) and return:

```json
{ "items": [ ... ], "page": 1, "pageSize": 20, "totalCount": 42, "totalPages": 3 }
```

Errors use RFC 7807 problem responses: `400` validation / bad reference,
`404` not found, `409` conflict (duplicate email, department name, skill or assignment).

## Migrations

```bash
dotnet ef migrations add <Name>      # create
dotnet ef migrations list            # list, pending ones are marked
dotnet ef database update            # apply
dotnet ef migrations remove          # undo the latest (only if not applied)
```

More detail is in `DeveloperGuide.txt`.  It also documents the initial errors encountered during development, along with their respective resolutions and workarounds.

## Tests

```bash
dotnet test EF.Tests                              # everything (needs Docker)
dotnet test EF.Tests --filter "Category!=Database" # no Docker needed
dotnet test EF.Tests --filter "Category=Database"  # database tests only
```

`EF.Tests` (xUnit + NSubstitute + Testcontainers) has two kinds of tests.

**Without a database** (144 tests):
- **Services** are unit-tested with mocked repositories: business rules, conflicts, not-found cases, delete guards, paging.
- **Exception handler** is tested with real Npgsql error objects (unique, restrict and foreign-key violations, client disconnects).
- **HTTP layer** is tested through the real ASP.NET pipeline (`WebApplicationFactory`) with the services mocked: routes, validation, status codes and the error JSON shape.

**With a real PostgreSQL** (64 tests, category `Database`). A throwaway PostgreSQL
container is started with Testcontainers, your real migrations are applied to it, and
it is removed afterwards. Your development database is never touched.
- **Schema:** every migration applies to an empty database, the migrations match the current model, `citext` is installed.
- **Constraints:** case-insensitive unique email and department name, unique skill names, the check on years of experience, column lengths, salary precision, the default joining date, and that referenced rows cannot be deleted.
- **Repositories:** real paging, ordering, includes and case-insensitive lookups.
- **End to end:** HTTP requests through the whole unmocked app into the database, including the blocked deletes and the simultaneous-duplicate email and department-name races.

Requirements for the database tests: Docker, and your user in the `docker` group
(`sudo usermod -aG docker $USER`, then log out and back in). The first run pulls the image.

PostgreSQL versions report some errors differently (a blocked delete is error 23001 on
PostgreSQL 18 but 23503 on 17), so check against more than one version:

```bash
TEST_POSTGRES_IMAGE=postgres:17 dotnet test EF.Tests   # default image is postgres:18
```

### Looking at the test database

The test container is deleted when the run ends. To keep it so you can inspect it, set `TEST_POSTGRES_KEEP=1`.
Run a test that leaves data behind (most tests clean up or are reset by the next one), for example:

```bash
TEST_POSTGRES_KEEP=1 dotnet test EF.Tests --filter "FullyQualifiedName~Paging_over_25_employees"
```

Then connect, using either:

```bash
docker exec -it ef-tests-postgres psql -U postgres        # no psql install needed
psql -h localhost -p 54329 -U postgres                     # from your machine; password: postgres
```

A GUI tool (DBeaver, pgAdmin, the VS Code PostgreSQL extension) works with host `localhost`, port `54329`,
database `postgres`, user `postgres`, password `postgres`. Useful queries: `\dt`, `select * from "Employees";`.

Each test run empties the tables first, so you see what the last test left behind. Running again replaces the kept container
automatically. Remove it when you are done: `docker rm -f ef-tests-postgres`.

### Docker cheat sheet

Run from the repo root. If you get `permission denied` on the Docker socket, your terminal does not have the `docker`
group yet: log out and back in, or prefix a command with `sg docker -c '...'`.

**Containers and images**

```bash
docker ps                          # running containers
docker ps -a                       # all containers, including stopped
docker images                      # downloaded images
docker logs ef-tests-postgres      # container output
docker stop ef-tests-postgres      # stop it
docker start ef-tests-postgres     # start a stopped one
docker rm -f ef-tests-postgres     # stop and delete it
docker pull postgres:18            # download an image by hand (tests do this automatically)
docker rmi postgres:17             # delete an image
docker system df                   # disk used by Docker
docker system prune                # remove stopped containers and unused data
```

**Running the tests**

```bash
dotnet test EF.Tests                                         # everything (needs Docker)
dotnet test EF.Tests --filter "Category=Database"            # database tests only
dotnet test EF.Tests --filter "Category!=Database"           # no Docker needed
TEST_POSTGRES_IMAGE=postgres:17 dotnet test EF.Tests         # another PostgreSQL version
TEST_POSTGRES_KEEP=1 dotnet test EF.Tests --filter "FullyQualifiedName~Skill_proficiency_and_years_round_trip"   # keep the container
```

**Connecting to the kept database** (user, password and database are all `postgres`)

```bash
docker exec -it ef-tests-postgres psql -U postgres           # shell inside the container
psql -h localhost -p 54329 -U postgres                        # from your machine
docker exec ef-tests-postgres psql -U postgres -c 'select * from "Employees";'   # one query
```

Connection string for a GUI or app: `Host=localhost;Port=54329;Database=postgres;Username=postgres;Password=postgres`.
These are the default credentials of the Testcontainers PostgreSQL module, not something set in this project.

**Inside psql.** Table names are case-sensitive, so keep the double quotes.

```sql
\dt                                  -- list tables
\d "Employees"                       -- columns, indexes and constraints
\q                                   -- quit

select * from "Employees";           -- also "Departments", "Projects", "Skills",
                                     -- "EmployeeSkills", "EmployeeProjects", "__EFMigrationsHistory"
select count(*) from "Employees";
select e."Name", d."Name" as dept
  from "Employees" e join "Departments" d on d."Id" = e."DepartmentId";
select s."Name", es."ProficiencyLevel", es."YearsOfExperience"
  from "EmployeeSkills" es join "Skills" s on s."Id" = es."SkillId";
```

**Deleting data.** This only affects the throwaway test database.

```sql
delete from "Skills" where "Id" = 1;                 -- one row (fails if still referenced)
truncate "EmployeeSkills";                           -- empty one table
truncate "EmployeeProjects", "EmployeeSkills", "Employees", "Departments", "Projects", "Skills"
  restart identity cascade;                          -- empty everything
drop table "EmployeeSkills";                         -- drop a table
drop table "Employees" cascade;                      -- also drops dependent objects
```

The simplest way to wipe everything is `docker rm -f ef-tests-postgres`; the next test run creates a fresh database with the full schema.
Your development database (port 5432, user secrets) is a separate server: do not run `drop` or `truncate` there.

The test project lives in a subfolder and is excluded from `EF.csproj`, so
`dotnet build`, `dotnet run` and `dotnet ef` at the repo root are unaffected.
