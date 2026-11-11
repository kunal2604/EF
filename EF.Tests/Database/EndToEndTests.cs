using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EF.Tests.Database;

/// <summary>
/// The whole application, unmocked: HTTP → controllers → services → repositories → PostgreSQL,
/// including the exception handler turning real database errors into responses.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public class EndToEndTests(PostgresFixture db) : DatabaseTestBase(db)
{
    private HttpClient Client => Db.Api.CreateClient();

    private static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();
    private static async Task<string> Detail(HttpResponseMessage r) => (await Json(r)).GetProperty("detail").GetString()!;

    private async Task<int> PostId(HttpClient c, string url, object body)
    {
        var response = await c.PostAsJsonAsync(url, body);
        Assert.True(response.IsSuccessStatusCode, $"POST {url} -> {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await Json(response)).GetProperty("id").GetInt32();
    }

    private Task<int> NewDepartment(HttpClient c, string name = "Engineering") => PostId(c, "/api/departments", new { name });

    private Task<int> NewEmployee(HttpClient c, int departmentId, string email = "asha@test.com") =>
        PostId(c, "/api/employees", new { name = "Asha", email, salary = 50000, joiningDate = "2024-01-15", departmentId });

    // ---- the whole lifecycle ----

    [Fact]
    public async Task Full_lifecycle_including_every_blocked_delete_and_how_to_unblock_it()
    {
        var c = Client;
        var dept = await NewDepartment(c);
        var emp = await NewEmployee(c, dept);
        var skill = await PostId(c, "/api/skills", new { name = "C#" });
        var project = await PostId(c, "/api/projects", new { name = "Apollo", description = "d" });

        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/employees/{emp}/skills",
            new { skillId = skill, proficiencyLevel = "Advanced", yearsOfExperience = 3 })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsJsonAsync($"/api/projects/{project}/employees", new { employeeId = emp })).StatusCode);

        // Everything is now in use, so every delete is refused with a message that says what to do.
        var delDept = await c.DeleteAsync($"/api/departments/{dept}");
        var delEmp = await c.DeleteAsync($"/api/employees/{emp}");
        var delSkill = await c.DeleteAsync($"/api/skills/{skill}");
        var delProject = await c.DeleteAsync($"/api/projects/{project}");
        Assert.All(new[] { delDept, delEmp, delSkill, delProject }, r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        Assert.Contains("employees belong to it", await Detail(delDept));
        Assert.Contains("assigned to projects", await Detail(delEmp));
        Assert.Contains("employees have it assigned", await Detail(delSkill));
        Assert.Contains("employees are assigned to it", await Detail(delProject));

        // Unassign, then everything can be deleted in turn.
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/employees/{emp}/skills/{skill}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/projects/{project}/employees/{emp}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/skills/{skill}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/projects/{project}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/employees/{emp}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/departments/{dept}")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/employees/{emp}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/departments/{dept}")).StatusCode);
    }

    [Fact]
    public async Task A_department_can_be_deleted_after_its_employees_move_away()
    {
        var c = Client;
        var from = await NewDepartment(c, "Old");
        var to = await NewDepartment(c, "New");
        var emp = await NewEmployee(c, from);

        Assert.Equal(HttpStatusCode.Conflict, (await c.DeleteAsync($"/api/departments/{from}")).StatusCode);

        var move = await c.PutAsJsonAsync($"/api/employees/{emp}",
            new { name = "Asha", email = "asha@test.com", salary = 50000, joiningDate = "2024-01-15", departmentId = to });
        Assert.Equal(HttpStatusCode.NoContent, move.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/departments/{from}")).StatusCode);
        Assert.Equal("New", (await Json(await c.GetAsync($"/api/employees/{emp}"))).GetProperty("departmentName").GetString());
    }

    // ---- uniqueness, as a client sees it ----

    [Theory]
    [InlineData("asha@test.com")]
    [InlineData("ASHA@TEST.COM")]
    public async Task A_duplicate_email_is_409_whatever_the_case(string duplicate)
    {
        var c = Client;
        var dept = await NewDepartment(c);
        await NewEmployee(c, dept, "asha@test.com");

        var response = await c.PostAsJsonAsync("/api/employees",
            new { name = "Other", email = duplicate, salary = 1, departmentId = dept });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Email already exists !", await Detail(response));
    }

    [Fact]
    public async Task Updating_to_someone_elses_email_is_409_but_keeping_your_own_is_fine()
    {
        var c = Client;
        var dept = await NewDepartment(c);
        var asha = await NewEmployee(c, dept, "asha@test.com");
        await NewEmployee(c, dept, "bob@test.com");
        object Body(string email) => new { name = "Asha", email, salary = 1, joiningDate = "2024-01-15", departmentId = dept };

        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/employees/{asha}", Body("BOB@test.com"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.PutAsJsonAsync($"/api/employees/{asha}", Body("ASHA@test.com"))).StatusCode);
    }

    [Theory]
    [InlineData("Engineering")]
    [InlineData("engineering")]
    public async Task A_duplicate_department_name_is_409_whatever_the_case(string duplicate)
    {
        var c = Client;
        await NewDepartment(c, "Engineering");

        var response = await c.PostAsJsonAsync("/api/departments", new { name = duplicate });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Department already exists !", await Detail(response));
    }

    [Fact]
    public async Task Renaming_to_another_departments_name_is_409_but_keeping_or_recasing_your_own_is_fine()
    {
        var c = Client;
        var engineering = await NewDepartment(c, "Engineering");
        await NewDepartment(c, "HR");

        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/departments/{engineering}", new { name = "hr" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.PutAsJsonAsync($"/api/departments/{engineering}", new { name = "Engineering" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.PutAsJsonAsync($"/api/departments/{engineering}", new { name = "ENGINEERING" })).StatusCode);
    }

    [Fact]
    public async Task Eight_simultaneous_creates_with_the_same_department_name_give_one_201_and_seven_409s_and_no_500s()
    {
        var c = Client;

        var responses = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => c.PostAsJsonAsync("/api/departments", new { name = "Legal" })));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(7, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task A_duplicate_skill_name_is_409_whatever_the_case()
    {
        var c = Client;
        await PostId(c, "/api/skills", new { name = "C#" });

        var response = await c.PostAsJsonAsync("/api/skills", new { name = "c#" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Skill already exists.", await Detail(response));
    }

    [Fact]
    public async Task Eight_simultaneous_creates_with_the_same_email_give_one_201_and_seven_409s_and_no_500s()
    {
        // The race the service-level check cannot stop: the unique index decides, the handler reports it.
        var c = Client;
        var dept = await NewDepartment(c);
        var body = new { name = "Race", email = "race@test.com", salary = 1, departmentId = dept };

        var codes = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
            (await c.PostAsJsonAsync("/api/employees", body)).StatusCode));

        Assert.Equal(1, codes.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(7, codes.Count(s => s == HttpStatusCode.Conflict));
        Assert.DoesNotContain(HttpStatusCode.InternalServerError, codes);
    }

    // ---- dates ----

    [Fact]
    public async Task Creating_without_a_date_stores_today_and_updating_without_one_is_refused()
    {
        var c = Client;
        var dept = await NewDepartment(c);

        var created = await c.PostAsJsonAsync("/api/employees", new { name = "A", email = "a@test.com", salary = 1, departmentId = dept });
        var json = await Json(created);
        var stored = DateOnly.Parse(json.GetProperty("joiningDate").GetString()!);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.InRange(stored, today.AddDays(-1), today.AddDays(1));

        var id = json.GetProperty("id").GetInt32();
        var update = await c.PutAsJsonAsync($"/api/employees/{id}", new { name = "A", email = "a@test.com", salary = 1, departmentId = dept });
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);

        // ...and the stored date was not wiped.
        var after = DateOnly.Parse((await Json(await c.GetAsync($"/api/employees/{id}"))).GetProperty("joiningDate").GetString()!);
        Assert.Equal(stored, after);
    }

    // ---- salary: validation and column agree ----

    [Fact]
    public async Task The_largest_accepted_salary_round_trips_and_one_digit_more_is_refused()
    {
        var c = Client;
        var dept = await NewDepartment(c);

        var ok = await c.PostAsync("/api/employees", new StringContent(
            $$"""{"name":"A","email":"max@test.com","salary":9999999999999999.99,"departmentId":{{dept}}}""",
            System.Text.Encoding.UTF8, "application/json"));
        var tooBig = await c.PostAsync("/api/employees", new StringContent(
            $$"""{"name":"B","email":"big@test.com","salary":10000000000000000,"departmentId":{{dept}}}""",
            System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooBig.StatusCode); // refused by validation, not by a database error
    }

    // ---- paging over real data ----

    [Fact]
    public async Task Paging_over_25_employees_returns_consistent_pages()
    {
        var c = Client;
        var dept = await NewDepartment(c);
        await using (var seed = NewContext())
        {
            for (var i = 1; i <= 25; i++)
                seed.Employees.Add(new EmployeeManagement.Models.Employee
                {
                    Name = $"E{i:00}", Email = $"e{i:00}@test.com", Salary = 1,
                    JoiningDate = new DateOnly(2024, 1, 1), DepartmentId = dept
                });
            await seed.SaveChangesAsync();
        }

        var page1 = await Json(await c.GetAsync("/api/employees?page=1&pageSize=10"));
        var page3 = await Json(await c.GetAsync("/api/employees?page=3&pageSize=10"));
        var defaults = await Json(await c.GetAsync("/api/employees"));

        Assert.Equal(25, page1.GetProperty("totalCount").GetInt32());
        Assert.Equal(3, page1.GetProperty("totalPages").GetInt32());
        Assert.Equal(10, page1.GetProperty("items").GetArrayLength());
        Assert.Equal("E01", page1.GetProperty("items")[0].GetProperty("name").GetString());
        Assert.Equal(5, page3.GetProperty("items").GetArrayLength());
        Assert.Equal("E25", page3.GetProperty("items")[4].GetProperty("name").GetString());
        Assert.Equal(20, defaults.GetProperty("pageSize").GetInt32());
        Assert.Equal(20, defaults.GetProperty("items").GetArrayLength());
    }

    // ---- assignment rules ----

    [Fact]
    public async Task Assigning_twice_is_409_and_unassigning_something_not_assigned_is_404()
    {
        var c = Client;
        var dept = await NewDepartment(c);
        var emp = await NewEmployee(c, dept);
        var skill = await PostId(c, "/api/skills", new { name = "C#" });
        var project = await PostId(c, "/api/projects", new { name = "Apollo", description = "d" });
        var assignSkill = new { skillId = skill, proficiencyLevel = "Basic", yearsOfExperience = 1 };

        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/employees/{emp}/skills", assignSkill)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/employees/{emp}/skills", assignSkill)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsJsonAsync($"/api/projects/{project}/employees", new { employeeId = emp })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/projects/{project}/employees", new { employeeId = emp })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/projects/{project}/employees/{emp}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.DeleteAsync($"/api/projects/{project}/employees/{emp}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/employees/{emp}/skills/{skill}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.DeleteAsync($"/api/employees/{emp}/skills/{skill}")).StatusCode);
    }

    [Fact]
    public async Task Skill_proficiency_and_years_round_trip_and_update()
    {
        var c = Client;
        var dept = await NewDepartment(c);
        var emp = await NewEmployee(c, dept);
        var skill = await PostId(c, "/api/skills", new { name = "SQL" });
        await c.PostAsJsonAsync($"/api/employees/{emp}/skills", new { skillId = skill, proficiencyLevel = "Basic", yearsOfExperience = 1 });

        Assert.Equal(HttpStatusCode.NoContent, (await c.PutAsJsonAsync($"/api/employees/{emp}/skills/{skill}",
            new { proficiencyLevel = "Expert", yearsOfExperience = 9 })).StatusCode);

        var list = await Json(await c.GetAsync($"/api/employees/{emp}/skills"));
        Assert.Equal("SQL", list[0].GetProperty("skillName").GetString());
        Assert.Equal("Expert", list[0].GetProperty("proficiencyLevel").GetString());
        Assert.Equal(9, list[0].GetProperty("yearsOfExperience").GetInt32());
    }
}
