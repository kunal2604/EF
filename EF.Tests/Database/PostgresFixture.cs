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
    /// Override with the TEST_POSTGRES_IMAGE environment variable, e.g. postgres:18.
    /// PostgreSQL versions differ in error codes (see ConstraintTests), so run both.
    /// </summary>
    public static string Image { get; } =
        Environment.GetEnvironmentVariable("TEST_POSTGRES_IMAGE") is { Length: > 0 } image ? image : "postgres:17";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(Image).Build();

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
