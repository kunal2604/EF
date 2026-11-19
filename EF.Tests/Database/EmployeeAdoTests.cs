using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EmployeeManagement.Models;
using EmployeeManagement.Repositories.AdoNet;
using Microsoft.Extensions.Configuration;

namespace EF.Tests.Database;

/// <summary>
/// The hand-written ADO.NET queries (Npgsql, no EF Core) against a real PostgreSQL: column mapping,
/// the id parameter, missing rows, and agreement with the EF Core endpoints.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public class EmployeeAdoRepositoryTests(PostgresFixture db) : DatabaseTestBase(db)
{
    private EmployeeAdoRepository NewRepository() => new(Configuration(Db.ConnectionString));

    private static IConfiguration Configuration(string connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = connectionString })
            .Build();

    private async Task<Employee> SeedAsync(int departmentId, string name, string email, decimal salary, DateOnly joined)
    {
        await using var ctx = NewContext();
        var employee = new Employee { Name = name, Email = email, Salary = salary, JoiningDate = joined, DepartmentId = departmentId };
        ctx.Employees.Add(employee);
        await ctx.SaveChangesAsync();
        return employee;
    }

    // ---- GetEmployeeByIdAsync ----

    [Fact]
    public async Task GetById_maps_every_column()
    {
        var department = await SeedDepartmentAsync();
        var seeded = await SeedAsync(department.Id, "Asha Rao", "Asha@Test.com", 12345.67m, new DateOnly(2023, 2, 28));

        var found = await NewRepository().GetEmployeeByIdAsync(seeded.Id);

        Assert.NotNull(found);
        Assert.Equal(seeded.Id, found.Id);
        Assert.Equal("Asha Rao", found.Name);
        Assert.Equal("Asha@Test.com", found.Email);          // the citext column keeps the stored case
        Assert.Equal(12345.67m, found.Salary);               // numeric(18,2) keeps both decimals
        Assert.Equal(new DateOnly(2023, 2, 28), found.JoiningDate);
        Assert.Equal(department.Id, found.DepartmentId);
    }

    [Fact]
    public async Task GetById_returns_null_for_an_id_that_does_not_exist()
    {
        var department = await SeedDepartmentAsync();
        var seeded = await SeedEmployeeAsync(department.Id);

        Assert.Null(await NewRepository().GetEmployeeByIdAsync(seeded.Id + 1000));
        Assert.Null(await NewRepository().GetEmployeeByIdAsync(0));
        Assert.Null(await NewRepository().GetEmployeeByIdAsync(-1));
    }

    [Fact]
    public async Task GetById_returns_only_the_requested_employee()
    {
        var department = await SeedDepartmentAsync();
        var first = await SeedEmployeeAsync(department.Id, "first@test.com", "First");
        var second = await SeedEmployeeAsync(department.Id, "second@test.com", "Second");

        var found = await NewRepository().GetEmployeeByIdAsync(second.Id);

        Assert.Equal("Second", found!.Name);
        Assert.NotEqual(first.Id, found.Id);
    }

    [Fact]
    public async Task GetById_does_not_load_relations_because_it_reads_the_employees_table_only()
    {
        var department = await SeedDepartmentAsync();
        var seeded = await SeedEmployeeAsync(department.Id);

        var found = await NewRepository().GetEmployeeByIdAsync(seeded.Id);

        Assert.Null(found!.Department);
        Assert.Empty(found.Projects);
        Assert.Empty(found.EmployeeSkills);
    }

    // ---- GetAllEmployeesAsync ----

    [Fact]
    public async Task GetAll_returns_an_empty_list_for_an_empty_table()
    {
        await SeedDepartmentAsync();

        Assert.Empty(await NewRepository().GetAllEmployeesAsync());
    }

    [Fact]
    public async Task GetAll_returns_every_employee_with_all_columns_mapped()
    {
        var engineering = await SeedDepartmentAsync("Engineering");
        var hr = await SeedDepartmentAsync("HR");
        var a = await SeedAsync(engineering.Id, "Asha", "asha@test.com", 100.50m, new DateOnly(2020, 1, 1));
        var b = await SeedAsync(hr.Id, "Ben", "ben@test.com", 99999999999999.99m, new DateOnly(2024, 12, 31));

        var all = await NewRepository().GetAllEmployeesAsync();

        // Order is not guaranteed (the query has no ORDER BY), so compare by id.
        var byId = all.ToDictionary(e => e.Id);
        Assert.Equal(2, all.Count);
        Assert.Equal(("Asha", "asha@test.com", 100.50m, new DateOnly(2020, 1, 1), engineering.Id),
            (byId[a.Id].Name, byId[a.Id].Email, byId[a.Id].Salary, byId[a.Id].JoiningDate, byId[a.Id].DepartmentId));
        Assert.Equal(("Ben", "ben@test.com", 99999999999999.99m, new DateOnly(2024, 12, 31), hr.Id),
            (byId[b.Id].Name, byId[b.Id].Email, byId[b.Id].Salary, byId[b.Id].JoiningDate, byId[b.Id].DepartmentId));
    }

    [Fact]
    public async Task GetAll_returns_every_row_even_when_there_are_many()
    {
        var department = await SeedDepartmentAsync();
        await using (var ctx = NewContext())
        {
            ctx.Employees.AddRange(Enumerable.Range(1, 60).Select(i => new Employee
            {
                Name = $"Emp{i:00}", Email = $"emp{i:00}@test.com", Salary = i,
                JoiningDate = new DateOnly(2024, 1, 15), DepartmentId = department.Id
            }));
            await ctx.SaveChangesAsync();
        }

        var all = await NewRepository().GetAllEmployeesAsync();

        // No paging: unlike /api/employees, this endpoint returns everything in one response.
        Assert.Equal(60, all.Count);
        Assert.Equal(60, all.Select(e => e.Id).Distinct().Count());
    }

    [Fact]
    public async Task Rows_written_by_the_ado_queries_view_match_what_ef_core_reads()
    {
        var department = await SeedDepartmentAsync();
        var seeded = await SeedAsync(department.Id, "Asha", "asha@test.com", 4321.10m, new DateOnly(2022, 6, 30));
        await using var ctx = NewContext();
        var viaEf = await ctx.Employees.FindAsync(seeded.Id);

        var viaAdo = await NewRepository().GetEmployeeByIdAsync(seeded.Id);

        Assert.NotNull(viaEf);
        Assert.NotNull(viaAdo);
        Assert.Equal((viaEf.Id, viaEf.Name, viaEf.Email, viaEf.Salary, viaEf.JoiningDate, viaEf.DepartmentId),
            (viaAdo.Id, viaAdo.Name, viaAdo.Email, viaAdo.Salary, viaAdo.JoiningDate, viaAdo.DepartmentId));
    }

    // ---- connection handling ----

    [Fact]
    public async Task A_connection_that_cannot_be_opened_surfaces_as_an_exception_not_an_empty_result()
    {
        var unreachable = new EmployeeAdoRepository(Configuration(
            "Host=localhost;Port=1;Database=x;Username=u;Password=p;Timeout=2"));

        await Assert.ThrowsAnyAsync<Exception>(() => unreachable.GetAllEmployeesAsync());
        await Assert.ThrowsAnyAsync<Exception>(() => unreachable.GetEmployeeByIdAsync(1));
    }
}

/// <summary>Needs no database: how the repository reads its configuration.</summary>
public class EmployeeAdoRepositoryConfigurationTests
{
    [Fact]
    public void A_missing_connection_string_fails_at_construction_with_a_clear_message()
    {
        var empty = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<InvalidOperationException>(() => new EmployeeAdoRepository(empty));

        Assert.Contains("Connection string", ex.Message);
    }
}

/// <summary>The whole app, unmocked: the ADO.NET endpoints must agree with the EF Core ones.</summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public class EmployeeAdoEndToEndTests(PostgresFixture db) : DatabaseTestBase(db)
{
    private HttpClient Client => Db.Api.CreateClient();

    private static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    private async Task<int> CreateEmployeeAsync(HttpClient c, string email, string name = "Asha")
    {
        var dept = await c.PostAsJsonAsync("/api/departments", new { name = $"Dept-{email}" });
        var deptId = (await Json(dept)).GetProperty("id").GetInt32();
        var response = await c.PostAsJsonAsync("/api/employees",
            new { name, email, salary = 50000.25, joiningDate = "2024-01-15", departmentId = deptId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await Json(response)).GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task The_real_app_wires_up_the_ado_controller_service_and_repository()
    {
        var response = await Client.GetAsync("/api/ado/employees");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);   // proves DI resolves the whole chain
        Assert.Equal(0, (await Json(response)).GetArrayLength());
    }

    [Fact]
    public async Task An_employee_created_through_the_ef_endpoint_is_read_back_through_ado()
    {
        var c = Client;
        var id = await CreateEmployeeAsync(c, "asha@test.com");

        var response = await c.GetAsync($"/api/ado/employees/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Json(response);
        Assert.Equal(id, body.GetProperty("id").GetInt32());
        Assert.Equal("Asha", body.GetProperty("name").GetString());
        Assert.Equal("asha@test.com", body.GetProperty("email").GetString());
        Assert.Equal(50000.25m, body.GetProperty("salary").GetDecimal());
        Assert.Equal("2024-01-15", body.GetProperty("joiningDate").GetString());
    }

    [Fact]
    public async Task The_list_contains_every_created_employee_and_the_unknown_id_is_404()
    {
        var c = Client;
        var first = await CreateEmployeeAsync(c, "a@test.com", "A");
        var second = await CreateEmployeeAsync(c, "b@test.com", "B");

        var list = await Json(await c.GetAsync("/api/ado/employees"));

        Assert.Equal(new[] { first, second }.Order(),
            list.EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).Order());
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/ado/employees/{second + 500}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/ado/employees/not-a-number")).StatusCode);
    }

    [Fact]
    public async Task A_deleted_employee_disappears_from_the_ado_endpoints_too()
    {
        var c = Client;
        var id = await CreateEmployeeAsync(c, "gone@test.com");
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/employees/{id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/ado/employees/{id}")).StatusCode);
        Assert.Equal(0, (await Json(await c.GetAsync("/api/ado/employees"))).GetArrayLength());
    }
}
