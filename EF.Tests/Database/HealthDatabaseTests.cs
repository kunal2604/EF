using System.Net;

namespace EF.Tests.Database;

[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public class HealthDatabaseTests(PostgresFixture db) : DatabaseTestBase(db)
{
    [Fact]
    public async Task Health_is_200_healthy_when_the_database_is_reachable()
    {
        var response = await Db.Api.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
