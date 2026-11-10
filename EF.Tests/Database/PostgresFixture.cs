using EmployeeManagement.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EF.Tests.Database;

/// <summary>
/// One throwaway PostgreSQL container per test run, shared by every database test.
/// The real migrations are applied to it, so the tests run against the real schema
/// (citext, constraints, defaults) and never touch your development database.
/// Requires Docker. Skip these tests with: dotnet test --filter "Category!=Database"
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    /// <summary>
    /// Override with the TEST_POSTGRES_IMAGE environment variable, e.g. postgres:17.
    /// PostgreSQL versions differ in error codes (see ConstraintTests), so run both.
    /// </summary>
    public static string Image { get; } =
        Environment.GetEnvironmentVariable("TEST_POSTGRES_IMAGE") is { Length: > 0 } image ? image : "postgres:18";

    /// <summary>
    /// Set TEST_POSTGRES_KEEP=1 to leave the container running after the tests, so you can
    /// look at the data (see README). It is then named <see cref="KeptContainerName"/> and
    /// listens on localhost:<see cref="KeptHostPort"/>.
    /// </summary>
    public static bool Keep { get; } = Environment.GetEnvironmentVariable("TEST_POSTGRES_KEEP") is "1" or "true";

    public const string KeptContainerName = "ef-tests-postgres";
    public const int KeptHostPort = 54329;

    private readonly PostgreSqlContainer _container = CreateContainer();

    private static PostgreSqlContainer CreateContainer()
    {
        var builder = new PostgreSqlBuilder(Image);

        if (Keep)
        {
            RemoveLeftoverContainer(); // from a previous kept run
            builder = builder
                .WithName(KeptContainerName)
                .WithPortBinding(KeptHostPort, 5432)
                .WithCleanUp(false); // don't let Testcontainers' reaper remove it
        }

        return builder.Build();
    }

    private static void RemoveLeftoverContainer()
    {
        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("docker", $"rm -f {KeptContainerName}")
            {
                RedirectStandardOutput = true, RedirectStandardError = true
            });
            process?.WaitForExit(15_000);
        }
        catch
        {
            // No docker CLI on the path: the start below reports the real problem.
        }
    }

    public string ConnectionString => _container.GetConnectionString();

    /// <summary>The real app (real services, repositories and exception handler) on the container.</summary>
    public DbApiFactory Api { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = NewContext();
        await context.Database.MigrateAsync(); // applies the real migrations

        Api = new DbApiFactory(ConnectionString);
    }

    public AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);

    /// <summary>Empties every table so each test starts from a known state.</summary>
    public async Task ResetAsync()
    {
        await using var context = NewContext();
        await context.Database.ExecuteSqlRawAsync(
            """TRUNCATE "EmployeeProjects", "EmployeeSkills", "Employees", "Departments", "Projects", "Skills" RESTART IDENTITY CASCADE""");
    }

    public async Task DisposeAsync()
    {
        Api?.Dispose();

        // When kept, the container is deliberately left running for inspection.
        if (!Keep)
            await _container.DisposeAsync();
    }
}

public sealed class DbApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing"); // keeps user secrets (your real connection string) out
        builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
    }
}

[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Database";
}
