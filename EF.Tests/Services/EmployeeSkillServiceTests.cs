using EmployeeManagement.DTOs.EmployeeSkills;
using EmployeeManagement.Exceptions;
using EmployeeManagement.Models;
using EmployeeManagement.Repositories.Interfaces;
using EmployeeManagement.Services;
using NSubstitute;

namespace EF.Tests.Services;

public class EmployeeSkillServiceTests
{
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly ISkillRepository _skills = Substitute.For<ISkillRepository>();
    private readonly IEmployeeSkillRepository _assignments = Substitute.For<IEmployeeSkillRepository>();
    private readonly EmployeeSkillService _sut;
    private readonly CancellationToken _ct = new CancellationTokenSource().Token;

    public EmployeeSkillServiceTests() => _sut = new EmployeeSkillService(_employees, _skills, _assignments);

    private static AssignSkillDto Assign() => new() { SkillId = 1, ProficiencyLevel = "Advanced", YearsOfExperience = 3 };

    // ---- assign ----

    [Fact]
    public async Task Assign_throws_NotFound_when_employee_missing()
    {
        _employees.GetByIdAsync(1, _ct).Returns((Employee?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _sut.AssignSkillAsync(1, Assign(), _ct));
        Assert.Contains("Employee", ex.Message);
    }

    [Fact]
    public async Task Assign_throws_NotFound_when_skill_missing()
    {
        _employees.GetByIdAsync(1, _ct).Returns(TestData.Employee());
        _skills.GetByIdAsync(1, _ct).Returns((Skill?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _sut.AssignSkillAsync(1, Assign(), _ct));
        Assert.Contains("Skill", ex.Message);
    }

    [Fact]
    public async Task Assign_throws_Conflict_when_already_assigned()
    {
        _employees.GetByIdAsync(1, _ct).Returns(TestData.Employee());
        _skills.GetByIdAsync(1, _ct).Returns(TestData.Skill());
        _assignments.GetAsync(1, 1, _ct).Returns(TestData.EmployeeSkill());

        await Assert.ThrowsAsync<ConflictException>(() => _sut.AssignSkillAsync(1, Assign(), _ct));
        await _assignments.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Assign_saves_the_assignment()
    {
        _employees.GetByIdAsync(1, _ct).Returns(TestData.Employee());
        _skills.GetByIdAsync(1, _ct).Returns(TestData.Skill());
        _assignments.GetAsync(1, 1, _ct).Returns((EmployeeSkill?)null);

        await _sut.AssignSkillAsync(1, Assign(), _ct);

        await _assignments.Received(1).AddAsync(
            Arg.Is<EmployeeSkill>(a => a.EmployeeId == 1 && a.SkillId == 1
                && a.ProficiencyLevel == "Advanced" && a.YearsOfExperience == 3), _ct);
    }

    // ---- read ----

    [Fact]
    public async Task GetEmployeeSkills_throws_NotFound_when_employee_missing()
    {
        _employees.GetByIdAsync(1, _ct).Returns((Employee?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetEmployeeSkillsAsync(1, _ct));
    }

    [Fact]
    public async Task GetEmployeeSkills_maps_the_skill_name()
    {
        _employees.GetByIdAsync(1, _ct).Returns(TestData.Employee());
        _assignments.GetByEmployeeIdAsync(1, _ct).Returns(new List<EmployeeSkill> { TestData.EmployeeSkill() });

        var result = await _sut.GetEmployeeSkillsAsync(1, _ct);

        var item = Assert.Single(result);
        Assert.Equal("C#", item.SkillName);
        Assert.Equal("Advanced", item.ProficiencyLevel);
        Assert.Equal(3, item.YearsOfExperience);
    }

    // ---- update ----

    [Fact]
    public async Task Update_throws_NotFound_when_not_assigned()
    {
        _assignments.GetAsync(1, 1, _ct).Returns((EmployeeSkill?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.UpdateSkillAsync(1, 1, new UpdateEmployeeSkillDto { ProficiencyLevel = "x", YearsOfExperience = 1 }, _ct));
    }

    [Fact]
    public async Task Update_applies_the_new_values()
    {
        var assignment = TestData.EmployeeSkill();
        _assignments.GetAsync(1, 1, _ct).Returns(assignment);

        await _sut.UpdateSkillAsync(1, 1, new UpdateEmployeeSkillDto { ProficiencyLevel = "Expert", YearsOfExperience = 9 }, _ct);

        Assert.Equal("Expert", assignment.ProficiencyLevel);
        Assert.Equal(9, assignment.YearsOfExperience);
        await _assignments.Received(1).UpdateAsync(assignment, _ct);
    }

    // ---- remove ----

    [Fact]
    public async Task Remove_throws_NotFound_when_not_assigned()
    {
        _assignments.GetAsync(1, 1, _ct).Returns((EmployeeSkill?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.RemoveSkillAsync(1, 1, _ct));
        await _assignments.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }

    [Fact]
    public async Task Remove_deletes_the_assignment()
    {
        var assignment = TestData.EmployeeSkill();
        _assignments.GetAsync(1, 1, _ct).Returns(assignment);

        await _sut.RemoveSkillAsync(1, 1, _ct);

        await _assignments.Received(1).DeleteAsync(assignment, _ct);
    }
}
