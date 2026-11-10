using EmployeeManagement.Mappings;

namespace EF.Tests.Mappings;

public class ResponseMappingsTests
{
    [Fact]
    public void Department_maps_id_and_name()
    {
        var dto = TestData.Department(7, "HR").ToResponseDto();

        Assert.Equal(7, dto.Id);
        Assert.Equal("HR", dto.Name);
    }

    [Fact]
    public void Employee_maps_every_field_and_the_department_name()
    {
        var dto = TestData.Employee(3, "x@test.com", TestData.Department(2, "Platform")).ToResponseDto();

        Assert.Equal(3, dto.Id);
        Assert.Equal("Asha", dto.Name);
        Assert.Equal("x@test.com", dto.Email);
        Assert.Equal(50000m, dto.Salary);
        Assert.Equal(new DateOnly(2024, 1, 15), dto.JoiningDate);
        Assert.Equal("Platform", dto.DepartmentName);
    }

    [Fact]
    public void Project_maps_member_names()
    {
        var project = TestData.Project(4, "Zeus");
        project.Employees.Add(TestData.Employee(1));
        project.Employees.Add(TestData.Employee(2, "b@test.com"));

        var dto = project.ToResponseDto();

        Assert.Equal(4, dto.Id);
        Assert.Equal("Zeus", dto.Name);
        Assert.Equal("desc", dto.Description);
        Assert.Equal(2, dto.EmployeeNames.Count);
    }

    [Fact]
    public void Project_without_employees_maps_an_empty_list()
    {
        Assert.Empty(TestData.Project().ToResponseDto().EmployeeNames);
    }

    [Fact]
    public void Skill_maps_id_and_name()
    {
        var dto = TestData.Skill(5, "Go").ToResponseDto();

        Assert.Equal(5, dto.Id);
        Assert.Equal("Go", dto.Name);
    }

    [Fact]
    public void EmployeeSkill_maps_the_skill_name_and_levels()
    {
        var dto = TestData.EmployeeSkill(9, TestData.Skill(2, "SQL")).ToResponseDto();

        Assert.Equal(9, dto.EmployeeId);
        Assert.Equal(2, dto.SkillId);
        Assert.Equal("SQL", dto.SkillName);
        Assert.Equal("Advanced", dto.ProficiencyLevel);
        Assert.Equal(3, dto.YearsOfExperience);
    }
}
