using System.Net;
using System.Net.Http.Json;
using EmployeeManagement.DTOs.Common;
using EmployeeManagement.DTOs.Employees;
using EmployeeManagement.Exceptions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace EF.Tests.Api;

public class EmployeesApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    private static readonly object ValidBody = new
    {
        name = "Asha", email = "asha@test.com", salary = 50000, joiningDate = "2024-01-15", departmentId = 1
    };

    private static EmployeeResponseDto Dto(int id = 1) => new()
    {
        Id = id, Name = "Asha", Email = "asha@test.com", Salary = 50000m,
        JoiningDate = new DateOnly(2024, 1, 15), DepartmentName = "Engineering"
    };

    // ---- validation: bad input never reaches the service ----

    [Theory]
    [InlineData("""{"name":"","email":"a@b.com","salary":1,"departmentId":1}""", "Name")]
    [InlineData("""{"name":"A","email":"not-an-email","salary":1,"departmentId":1}""", "Email")]
    [InlineData("""{"name":"A","email":"a@b.com","salary":-5,"departmentId":1}""", "Salary")]
    [InlineData("""{"name":"A","email":"a@b.com","salary":1,"departmentId":0}""", "DepartmentId")]
    public async Task Create_rejects_invalid_bodies_with_400(string body, string field)
    {
        var response = await Client.PostAsync("/api/employees", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(field, await ErrorFields(response));
        await Factory.Employees.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Fact]
    public async Task Create_rejects_a_name_longer_than_the_column()
    {
        var response = await Client.PostAsJsonAsync("/api/employees",
            new { name = new string('x', 101), email = "a@b.com", salary = 1, departmentId = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_accepts_a_salary_up_to_the_column_limit()
    {
        Factory.Employees.CreateAsync(Arg.Any<CreateEmployeeDto>(), Arg.Any<CancellationToken>()).Returns(Dto());

        var response = await Client.PostAsync("/api/employees", new StringContent(
            """{"name":"A","email":"a@b.com","salary":9999999999999999.99,"departmentId":1}""",
            System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Update_without_joiningDate_is_400_but_create_without_it_is_fine()
    {
        Factory.Employees.CreateAsync(Arg.Any<CreateEmployeeDto>(), Arg.Any<CancellationToken>()).Returns(Dto());
        var noDate = new { name = "A", email = "a@b.com", salary = 1, departmentId = 1 };

        var create = await Client.PostAsJsonAsync("/api/employees", noDate);
        var update = await Client.PutAsJsonAsync("/api/employees/1", noDate);

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        Assert.Contains("JoiningDate", await ErrorFields(update));
    }

    [Fact]
    public async Task Property_names_are_case_insensitive()
    {
        Factory.Employees.CreateAsync(Arg.Any<CreateEmployeeDto>(), Arg.Any<CancellationToken>()).Returns(Dto());

        var response = await Client.PostAsJsonAsync("/api/employees",
            new { Name = "A", EMAIL = "a@b.com", salary = 1, DepartmentId = 1 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ---- success and error mapping ----

    [Fact]
    public async Task Create_returns_201_with_a_location_header()
    {
        Factory.Employees.CreateAsync(Arg.Any<CreateEmployeeDto>(), Arg.Any<CancellationToken>()).Returns(Dto(7));

        var response = await Client.PostAsJsonAsync("/api/employees", ValidBody);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.EndsWith("/api/Employees/7", response.Headers.Location!.ToString());
        Assert.Equal("Engineering", (await Json(response)).GetProperty("departmentName").GetString());
    }

    [Fact]
    public async Task A_Conflict_from_the_service_becomes_409_problem_json()
    {
        Factory.Employees.CreateAsync(Arg.Any<CreateEmployeeDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictException("Email already exists !"));

        var response = await Client.PostAsJsonAsync("/api/employees", ValidBody);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Email already exists !", await Detail(response));
    }

    [Fact]
    public async Task A_BadRequest_from_the_service_becomes_400()
    {
        Factory.Employees.CreateAsync(Arg.Any<CreateEmployeeDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new BadRequestException("Department doesn't exist !"));

        var response = await Client.PostAsJsonAsync("/api/employees", ValidBody);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Department doesn't exist !", await Detail(response));
    }

    [Fact]
    public async Task An_unexpected_exception_is_500_and_does_not_leak()
    {
        Factory.Employees.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("secret connection detail"));

        var response = await Client.GetAsync("/api/employees/1");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("secret", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_by_id_is_404_when_missing_and_200_when_found()
    {
        Factory.Employees.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Dto());
        Factory.Employees.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns((EmployeeResponseDto?)null);

        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync("/api/employees/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/employees/2")).StatusCode);
    }

    [Fact]
    public async Task Update_maps_false_to_404_and_true_to_204()
    {
        var body = new { name = "A", email = "a@b.com", salary = 1, joiningDate = "2024-01-01", departmentId = 1 };
        Factory.Employees.UpdateAsync(1, Arg.Any<UpdateEmployeeDto>(), Arg.Any<CancellationToken>()).Returns(true);
        Factory.Employees.UpdateAsync(2, Arg.Any<UpdateEmployeeDto>(), Arg.Any<CancellationToken>()).Returns(false);

        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/employees/1", body)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.PutAsJsonAsync("/api/employees/2", body)).StatusCode);
    }

    [Fact]
    public async Task Delete_maps_false_to_404_true_to_204_and_conflict_to_409()
    {
        Factory.Employees.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        Factory.Employees.DeleteAsync(2, Arg.Any<CancellationToken>()).Returns(false);
        Factory.Employees.DeleteAsync(3, Arg.Any<CancellationToken>()).ThrowsAsync(new ConflictException("assigned to projects"));

        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync("/api/employees/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.DeleteAsync("/api/employees/2")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.DeleteAsync("/api/employees/3")).StatusCode);
    }

    // ---- paging ----

    [Fact]
    public async Task List_uses_page_1_and_size_20_by_default()
    {
        Factory.Employees.GetPagedAsync(Arg.Any<PagedQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<EmployeeResponseDto>());

        await Client.GetAsync("/api/employees");

        await Factory.Employees.Received(1).GetPagedAsync(
            Arg.Is<PagedQuery>(q => q.Page == 1 && q.PageSize == 20), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_passes_the_requested_page()
    {
        Factory.Employees.GetPagedAsync(Arg.Any<PagedQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<EmployeeResponseDto>());

        await Client.GetAsync("/api/employees?page=3&pageSize=50");

        await Factory.Employees.Received(1).GetPagedAsync(
            Arg.Is<PagedQuery>(q => q.Page == 3 && q.PageSize == 50), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=-5")]
    [InlineData("page=abc")]
    [InlineData("page=2147483647")]
    public async Task List_rejects_bad_paging_values_with_400(string query)
    {
        var response = await Client.GetAsync($"/api/employees?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await Factory.Employees.DidNotReceiveWithAnyArgs().GetPagedAsync(default!, default);
    }

    [Fact]
    public async Task List_returns_the_paging_envelope()
    {
        Factory.Employees.GetPagedAsync(Arg.Any<PagedQuery>(), Arg.Any<CancellationToken>()).Returns(
            new PagedResult<EmployeeResponseDto> { Items = [Dto()], Page = 1, PageSize = 20, TotalCount = 1, TotalPages = 1 });

        var json = await Json(await Client.GetAsync("/api/employees"));

        Assert.Equal(1, json.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, json.GetProperty("items").GetArrayLength());
        Assert.True(json.TryGetProperty("totalPages", out _));
    }
}
