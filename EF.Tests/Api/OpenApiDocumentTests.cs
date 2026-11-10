using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace EF.Tests.Api;

/// <summary>The OpenAPI document is only served in Development, so this factory switches to it.</summary>
public class OpenApiFactory : ApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseEnvironment("Development");
    }
}

/// <summary>Swagger should list the error responses, not only the success one.</summary>
public class OpenApiDocumentTests : IClassFixture<OpenApiFactory>
{
    private readonly OpenApiFactory _factory;

    public OpenApiDocumentTests(OpenApiFactory factory) => _factory = factory;

    private async Task<JsonElement> Paths()
    {
        var json = await _factory.CreateClient().GetStringAsync("/openapi/v1.json");
        return JsonDocument.Parse(json).RootElement.GetProperty("paths");
    }

    private static string[] Codes(JsonElement paths, string path, string method) =>
        paths.GetProperty(path).GetProperty(method).GetProperty("responses")
            .EnumerateObject().Select(p => p.Name).Order().ToArray();

    [Theory]
    [InlineData("/api/Employees", "get", new[] { "200", "400", "500" })]
    [InlineData("/api/Employees", "post", new[] { "201", "400", "409", "500" })]
    [InlineData("/api/Employees/{id}", "get", new[] { "200", "404", "500" })]
    [InlineData("/api/Employees/{id}", "put", new[] { "204", "400", "404", "409", "500" })]
    [InlineData("/api/Employees/{id}", "delete", new[] { "204", "404", "409", "500" })]
    [InlineData("/api/Departments/{id}", "delete", new[] { "204", "404", "409", "500" })]
    [InlineData("/api/Skills", "post", new[] { "201", "400", "409", "500" })]
    [InlineData("/api/Projects/{projectId}/employees", "post", new[] { "204", "400", "404", "409", "500" })]
    [InlineData("/api/employees/{employeeId}/skills", "post", new[] { "200", "400", "404", "409", "500" })]
    [InlineData("/api/employees/{employeeId}/skills/{skillId}", "delete", new[] { "204", "404", "500" })]
    public async Task Endpoint_documents_its_success_and_error_responses(string path, string method, string[] expected)
        => Assert.Equal(expected, Codes(await Paths(), path, method));

    [Fact]
    public async Task Every_operation_documents_the_500_error()
    {
        var paths = await Paths();
        var missing = (from p in paths.EnumerateObject()
                       from op in p.Value.EnumerateObject()
                       where !op.Value.GetProperty("responses").TryGetProperty("500", out _)
                       select $"{op.Name.ToUpper()} {p.Name}").ToList();
        Assert.Empty(missing);
    }

    [Fact]
    public async Task Error_responses_are_described_as_problem_details()
    {
        var paths = await Paths();
        var schema = paths.GetProperty("/api/Employees/{id}").GetProperty("get").GetProperty("responses")
            .GetProperty("404").GetProperty("content").GetProperty("application/problem+json").GetProperty("schema");
        Assert.Contains("ProblemDetails", schema.GetProperty("$ref").GetString());
    }
}
