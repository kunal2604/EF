using System.Net.Http.Json;
using System.Text.Json;

namespace EF.Tests.Api;

public abstract class ApiTestBase : IClassFixture<ApiFactory>
{
    protected readonly ApiFactory Factory;
    protected readonly HttpClient Client;

    protected ApiTestBase(ApiFactory factory)
    {
        Factory = factory;
        Factory.ResetMocks(); // the fixture is shared by a class's tests; start each from clean mocks
        Client = factory.CreateClient();
    }

    protected static async Task<JsonElement> Json(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    protected static async Task<string> Detail(HttpResponseMessage response) =>
        (await Json(response)).GetProperty("detail").GetString()!;

    protected static async Task<string[]> ErrorFields(HttpResponseMessage response) =>
        (await Json(response)).GetProperty("errors").EnumerateObject().Select(p => p.Name).ToArray();
}
