using System.Net;
using System.Text.Json;
using EmployeeManagement.Models;
using NSubstitute;

namespace EF.Tests.Api;

/// <summary>The ADO.NET endpoints under /api/ado/employees, through the real ASP.NET pipeline.</summary>
public class EmployeeAdoApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Get_all_returns_200_with_every_employee_as_json()
    {
        Factory.EmployeeAdo.GetAllEmployeesAsync().Returns(new List<Employee>
        {
            TestData.Employee(1), TestData.Employee(2, "b@test.com")
        });

        var response = await Client.GetAsync("/api/ado/employees");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        var items = (await Json(response)).EnumerateArray().ToList();
        Assert.Equal(new[] { 1, 2 }, items.Select(i => i.GetProperty("id").GetInt32()));
        Assert.Equal("b@test.com", items[1].GetProperty("email").GetString());
    }

    [Fact]
    public async Task Get_all_returns_200_and_an_empty_array_when_there_are_none()
    {
        Factory.EmployeeAdo.GetAllEmployeesAsync().Returns(new List<Employee>());

        var response = await Client.GetAsync("/api/ado/employees");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Json(response);
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Equal(0, body.GetArrayLength());
    }

    [Fact]
    public async Task Get_by_id_returns_200_with_the_employees_columns()
    {
        Factory.EmployeeAdo.GetEmployeeByIdAsync(1).Returns(TestData.Employee(1));

        var response = await Client.GetAsync("/api/ado/employees/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Json(response);
        Assert.Equal(1, body.GetProperty("id").GetInt32());
        Assert.Equal("Asha", body.GetProperty("name").GetString());
        Assert.Equal("asha@test.com", body.GetProperty("email").GetString());
        Assert.Equal(50000m, body.GetProperty("salary").GetDecimal());
        Assert.Equal("2024-01-15", body.GetProperty("joiningDate").GetString());
        Assert.Equal(1, body.GetProperty("departmentId").GetInt32());
    }

    [Fact]
    public async Task Get_by_id_returns_404_when_the_employee_does_not_exist()
    {
        Factory.EmployeeAdo.GetEmployeeByIdAsync(99).Returns((Employee?)null);

        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/ado/employees/99")).StatusCode);
    }

    [Fact]
    public async Task Get_by_id_passes_the_id_from_the_url_to_the_service()
    {
        Factory.EmployeeAdo.GetEmployeeByIdAsync(Arg.Any<int>()).Returns((Employee?)null);

        await Client.GetAsync("/api/ado/employees/42");

        await Factory.EmployeeAdo.Received(1).GetEmployeeByIdAsync(42);
    }

    // The route is "{id:int}": anything that is not a whole number must not match at all, so
    // the service is never called. (Without ":int" Swagger UI also refuses to send the request.)
    [Theory]
    [InlineData("abc")]
    [InlineData("1.5")]
    [InlineData("99999999999")] // too big for an int
    public async Task Get_by_id_with_a_non_integer_id_is_404_and_never_reaches_the_service(string id)
    {
        var response = await Client.GetAsync($"/api/ado/employees/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await Factory.EmployeeAdo.DidNotReceiveWithAnyArgs().GetEmployeeByIdAsync(default);
    }

    [Fact]
    public async Task A_failure_in_the_data_layer_becomes_a_generic_500_without_details()
    {
        Factory.EmployeeAdo.GetAllEmployeesAsync().Returns<Task<List<Employee>>>(
            _ => throw new InvalidOperationException("connection string password=hunter2"));

        var response = await Client.GetAsync("/api/ado/employees");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("hunter2", body);
    }
}
