using EmployeeManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace EF.Tests.Database;

[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public class SchemaTests(PostgresFixture db) : DatabaseTestBase(db)
{
    [Fact]
    public async Task Every_migration_applies_to_an_empty_database()
    {
        await using var ctx = NewContext();

        var all = ctx.Database.GetMigrations().ToList();
        var applied = (await ctx.Database.GetAppliedMigrationsAsync()).ToList();
        var pending = (await ctx.Database.GetPendingMigrationsAsync()).ToList();

        Assert.NotEmpty(all);
        Assert.Equal(all, applied);
        Assert.Empty(pending);
    }

    [Fact]
    public async Task The_migrations_match_the_current_model()
    {
        // Fails if someone changes an entity or the DbContext and forgets `dotnet ef migrations add`.
        await using var ctx = NewContext();

        Assert.False(ctx.Database.HasPendingModelChanges(),
            "The model has changes that no migration covers. Run: dotnet ef migrations add <Name>");
    }

    [Fact]
    public async Task The_citext_extension_is_installed()
    {
        await using var ctx = NewContext();

        var count = await ctx.Database
            .SqlQueryRaw<int>("""SELECT count(*)::int AS "Value" FROM pg_extension WHERE extname = 'citext'""")
            .SingleAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Email_column_is_citext()
    {
        await using var ctx = NewContext();

        var type = await ctx.Database
            .SqlQueryRaw<string>("""
                SELECT data_type || ':' || udt_name AS "Value" FROM information_schema.columns
                WHERE table_name = 'Employees' AND column_name = 'Email'
                """)
            .SingleAsync();

        Assert.EndsWith("citext", type);
    }
}
