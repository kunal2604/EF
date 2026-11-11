using EmployeeManagement.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EF.Tests.Database;

/// <summary>
/// The Database:MigrateOnStartup switch (docker-compose turns it on) creates the schema in an empty
/// database when the app starts, and leaves it alone when the switch is off.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public class MigrateOnStartupTests(PostgresFixture db) : DatabaseTestBase(db)
{
    private async Task<string> NewEmptyDatabaseAsync(string name)
    {
        var admin = new NpgsqlConnectionStringBuilder(Db.ConnectionString);
        await using (var conn = new NpgsqlConnection(admin.ConnectionString))
        {
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS {name} WITH (FORCE); CREATE DATABASE {name}", conn);
            await cmd.ExecuteNonQueryAsync();
        }
        admin.Database = name;
        return admin.ConnectionString;
    }

    private static WebApplicationFactory<Program> Factory(string connectionString, bool migrate) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
            b.UseSetting("Database:MigrateOnStartup", migrate ? "true" : "false");
        });

    private static AppDbContext Context(string connectionString) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options);

    [Fact]
    public async Task When_on_starting_the_app_creates_every_table_in_an_empty_database()
    {
        var connectionString = await NewEmptyDatabaseAsync("migrate_on");
        await using (var factory = Factory(connectionString, migrate: true))
            _ = factory.CreateClient(); // starting the app runs the migrations

        await using var ctx = Context(connectionString);
        Assert.Empty(await ctx.Database.GetPendingMigrationsAsync());
        Assert.Equal(0, await ctx.Departments.CountAsync()); // the table exists and can be queried
    }

    [Fact]
    public async Task Starting_twice_is_harmless_because_applied_migrations_are_skipped()
    {
        var connectionString = await NewEmptyDatabaseAsync("migrate_twice");
        for (var i = 0; i < 2; i++)
            await using (var factory = Factory(connectionString, migrate: true))
                _ = factory.CreateClient();

        await using var ctx = Context(connectionString);
        Assert.Empty(await ctx.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task When_off_starting_the_app_leaves_the_database_empty()
    {
        var connectionString = await NewEmptyDatabaseAsync("migrate_off");
        await using (var factory = Factory(connectionString, migrate: false))
            _ = factory.CreateClient();

        await using var ctx = Context(connectionString);
        Assert.Empty(await ctx.Database.GetAppliedMigrationsAsync());
    }
}
