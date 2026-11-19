using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace EF.Tests.Api;

/// <summary>An app whose database cannot be reached (nothing listens on port 1).</summary>
public class UnreachableDatabaseFactory : ApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("ConnectionStrings:DefaultConnection",
            "Host=localhost;Port=1;Database=secretdb;Username=secretuser;Password=secretpass;Timeout=2;Command Timeout=2");
    }
}

public class HealthApiTests(UnreachableDatabaseFactory factory) : IClassFixture<UnreachableDatabaseFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Live_is_healthy_even_when_the_database_is_down()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Health_is_503_when_the_database_cannot_be_reached_and_says_nothing_else()
    {
        var response = await _client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", body);   // no host, user, password or exception text
    }
}
