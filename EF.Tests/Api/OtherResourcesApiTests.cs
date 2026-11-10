using System.Net;
using System.Net.Http.Json;
using EmployeeManagement.DTOs.Common;
using EmployeeManagement.DTOs.Departments;
using EmployeeManagement.DTOs.EmployeeSkills;
using EmployeeManagement.DTOs.Projects;
using EmployeeManagement.Exceptions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace EF.Tests.Api;

public class ProjectsApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task GET_list_is_routed_and_other_verbs_are_not()
    {
        // Regression: GetAll once had no [HttpGet] and answered to every verb.
        Factory.Projects.GetPagedAsync(Arg.Any<PagedQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProjectResponseDto>());

        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync("/api/projects")).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await Client.DeleteAsync("/api/projects")).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await Client.PutAsJsonAsync("/api/projects", new { })).StatusCode);
    }

    [Fact]
    public async Task Create_requires_a_name()
    {
        var response = await Client.PostAsJsonAsync("/api/projects", new { name = "", description = "d" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Name", await ErrorFields(response));
    }

    [Fact]
    public async Task Create_rejects_a_description_over_500_characters()
    {
        var response = await Client.PostAsJsonAsync("/api/projects", new { name = "A", description = new string('x', 501) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Assign_returns_204_and_NotFound_and_Conflict_map_correctly()
    {
        Factory.Projects.AssignEmployeeAsync(9, 1, Arg.Any<CancellationToken>()).ThrowsAsync(new NotFoundException("Project not found !"));
        Factory.Projects.AssignEmployeeAsync(1, 1, Arg.Any<CancellationToken>()).ThrowsAsync(new ConflictException("already assigned"));

        Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsJsonAsync("/api/projects/2/employees", new { employeeId = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.PostAsJsonAsync("/api/projects/9/employees", new { employeeId = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PostAsJsonAsync("/api/projects/1/employees", new { employeeId = 1 })).StatusCode);
    }

    [Fact]
    public async Task Assign_rejects_an_employee_id_below_1()
    {
        var response = await Client.PostAsJsonAsync("/api/projects/1/employees", new { employeeId = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unassign_route_exists_and_maps_NotFound()
    {
        Factory.Projects.RemoveEmployeeAsync(1, 2, Arg.Any<CancellationToken>()).ThrowsAsync(new NotFoundException("not assigned"));

        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync("/api/projects/1/employees/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.DeleteAsync("/api/projects/1/employees/2")).StatusCode);
    }

    [Fact]
    public async Task Delete_maps_false_to_404_and_conflict_to_409()
    {
        Factory.Projects.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        Factory.Projects.DeleteAsync(2, Arg.Any<CancellationToken>()).Returns(false);
        Factory.Projects.DeleteAsync(3, Arg.Any<CancellationToken>()).ThrowsAsync(new ConflictException("employees assigned"));

        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync("/api/projects/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.DeleteAsync("/api/projects/2")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.DeleteAsync("/api/projects/3")).StatusCode);
    }
}

public class SkillsApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Create_returns_201_with_the_new_id()
    {
        Factory.Skills.CreateAsync(Arg.Any<CreateSkillDto>(), Arg.Any<CancellationToken>()).Returns(5);

        var response = await Client.PostAsJsonAsync("/api/skills", new { name = "Go" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(5, (await Json(response)).GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task Create_with_a_blank_name_is_400_and_a_duplicate_is_409()
    {
        Factory.Skills.CreateAsync(Arg.Any<CreateSkillDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictException("Skill already exists."));

        Assert.Equal(HttpStatusCode.BadRequest, (await Client.PostAsJsonAsync("/api/skills", new { name = "" })).StatusCode);

        var duplicate = await Client.PostAsJsonAsync("/api/skills", new { name = "C#" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("Skill already exists.", await Detail(duplicate));
    }

    [Fact]
    public async Task Get_and_delete_map_missing_to_404()
    {
        Factory.Skills.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((SkillResponseDto?)null);
        Factory.Skills.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(false);

        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/skills/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.DeleteAsync("/api/skills/1")).StatusCode);
    }

    [Fact]
    public async Task Delete_of_a_skill_in_use_is_409()
    {
        Factory.Skills.DeleteAsync(2, Arg.Any<CancellationToken>()).ThrowsAsync(new ConflictException("in use"));

        Assert.Equal(HttpStatusCode.Conflict, (await Client.DeleteAsync("/api/skills/2")).StatusCode);
    }
}

public class DepartmentsApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Get_by_id_maps_missing_to_404_and_found_to_200()
    {
        Factory.Departments.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new DepartmentResponseDto { Id = 1, Name = "Engineering" });
        Factory.Departments.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns((DepartmentResponseDto?)null);

        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync("/api/departments/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/departments/2")).StatusCode);
    }

    [Theory]
    [InlineData("")]
    public async Task Create_and_update_require_a_name(string name)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.PostAsJsonAsync("/api/departments", new { name })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.PutAsJsonAsync("/api/departments/1", new { name })).StatusCode);
    }

    [Fact]
    public async Task Update_and_delete_map_false_to_404_and_true_to_204()
    {
        Factory.Departments.UpdateAsync(1, Arg.Any<UpdateDepartmentDto>(), Arg.Any<CancellationToken>()).Returns(true);
        Factory.Departments.UpdateAsync(2, Arg.Any<UpdateDepartmentDto>(), Arg.Any<CancellationToken>()).Returns(false);
        Factory.Departments.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        Factory.Departments.DeleteAsync(2, Arg.Any<CancellationToken>()).Returns(false);

        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/departments/1", new { name = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.PutAsJsonAsync("/api/departments/2", new { name = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync("/api/departments/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.DeleteAsync("/api/departments/2")).StatusCode);
    }
}

public class EmployeeSkillsApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Assign_validates_the_body()
    {
        var tooLong = await Client.PostAsJsonAsync("/api/employees/1/skills",
            new { skillId = 1, proficiencyLevel = new string('x', 31), yearsOfExperience = 1 });
        var negative = await Client.PostAsJsonAsync("/api/employees/1/skills",
            new { skillId = 1, proficiencyLevel = "Basic", yearsOfExperience = -1 });
        var badSkill = await Client.PostAsJsonAsync("/api/employees/1/skills",
            new { skillId = 0, proficiencyLevel = "Basic", yearsOfExperience = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, negative.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badSkill.StatusCode);
    }

    [Fact]
    public async Task Update_enforces_the_same_30_character_limit_as_the_column()
    {
        // Regression: this DTO once allowed 100 against a 30-character column.
        var response = await Client.PutAsJsonAsync("/api/employees/1/skills/1",
            new { proficiencyLevel = new string('x', 31), yearsOfExperience = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Service_errors_map_to_404_and_409()
    {
        var ok = new { skillId = 1, proficiencyLevel = "Basic", yearsOfExperience = 1 };
        Factory.EmployeeSkills.AssignSkillAsync(9, Arg.Any<AssignSkillDto>(), Arg.Any<CancellationToken>()).ThrowsAsync(new NotFoundException("Employee not found."));
        Factory.EmployeeSkills.AssignSkillAsync(2, Arg.Any<AssignSkillDto>(), Arg.Any<CancellationToken>()).ThrowsAsync(new ConflictException("already assigned"));

        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsJsonAsync("/api/employees/1/skills", ok)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.PostAsJsonAsync("/api/employees/9/skills", ok)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PostAsJsonAsync("/api/employees/2/skills", ok)).StatusCode);
    }

    [Fact]
    public async Task Unassign_route_exists_and_maps_NotFound()
    {
        Factory.EmployeeSkills.RemoveSkillAsync(1, 2, Arg.Any<CancellationToken>()).ThrowsAsync(new NotFoundException("not found"));

        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync("/api/employees/1/skills/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.DeleteAsync("/api/employees/1/skills/2")).StatusCode);
    }

    [Fact]
    public async Task Listing_a_missing_employees_skills_is_404()
    {
        Factory.EmployeeSkills.GetEmployeeSkillsAsync(9, Arg.Any<CancellationToken>()).ThrowsAsync(new NotFoundException("Employee not found."));

        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/employees/9/skills")).StatusCode);
    }
}
