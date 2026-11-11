using EmployeeManagement.DTOs.Common;
using EmployeeManagement.DTOs.Projects;
using EmployeeManagement.Exceptions;
using EmployeeManagement.Mappings;
using EmployeeManagement.Models;
using EmployeeManagement.Repositories.Interfaces;
using EmployeeManagement.Services;
using NSubstitute;

namespace EF.Tests.Services;

public class ProjectServiceTests
{
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly ProjectService _sut;
    private readonly CancellationToken _ct = new CancellationTokenSource().Token;

    public ProjectServiceTests() => _sut = new ProjectService(_projects, _employees);

    [Fact]
    public async Task GetPaged_includes_employee_names()
    {
        var project = TestData.Project();
        project.Employees.Add(TestData.Employee(1));
        _projects.GetPagedAsync(1, 20, _ct).Returns((new List<ProjectResponseDto> { project.ToResponseDto() }, 1));

        var result = await _sut.GetPagedAsync(new PagedQuery(), _ct);

        Assert.Equal(new[] { "Asha" }, result.Items.Single().EmployeeNames);
    }

    [Fact]
    public async Task GetById_returns_the_repository_dto()
    {
        var project = TestData.Project();
        project.Employees.Add(TestData.Employee(1));
        _projects.GetResponseByIdAsync(1, _ct).Returns(project.ToResponseDto());

        var dto = await _sut.GetByIdAsync(1, _ct);

        Assert.NotNull(dto);
        Assert.Equal(new[] { "Asha" }, dto.EmployeeNames);
        await _projects.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()); // no tracked entity needed to read
    }

    [Fact]
    public async Task GetById_returns_null_when_missing()
    {
        _projects.GetResponseByIdAsync(9, _ct).Returns((ProjectResponseDto?)null);

        Assert.Null(await _sut.GetByIdAsync(9, _ct));
    }

    [Fact]
    public async Task Create_saves_and_returns_an_empty_member_list()
    {
        var dto = await _sut.CreateAsync(new CreateProjectDto { Name = "Apollo", Description = "d" }, _ct);

        Assert.Equal("Apollo", dto.Name);
        Assert.Empty(dto.EmployeeNames);
        await _projects.Received(1).AddAsync(Arg.Is<Project>(p => p.Name == "Apollo"), _ct);
    }

    // ---- assign ----

    [Fact]
    public async Task Assign_throws_NotFound_when_project_missing()
    {
        _projects.GetByIdAsync(1, _ct).Returns((Project?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _sut.AssignEmployeeAsync(1, 1, _ct));
        Assert.Contains("Project", ex.Message);
    }

    [Fact]
    public async Task Assign_throws_NotFound_when_employee_missing()
    {
        _projects.GetByIdAsync(1, _ct).Returns(TestData.Project());
        _employees.GetByIdAsync(1, _ct).Returns((Employee?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _sut.AssignEmployeeAsync(1, 1, _ct));
        Assert.Contains("Employee", ex.Message);
    }

    [Fact]
    public async Task Assign_throws_Conflict_when_already_assigned()
    {
        var project = TestData.Project();
        var employee = TestData.Employee();
        employee.Projects.Add(project);
        _projects.GetByIdAsync(1, _ct).Returns(project);
        _employees.GetByIdAsync(1, _ct).Returns(employee);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.AssignEmployeeAsync(1, 1, _ct));
        await _employees.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task Assign_adds_the_project_and_saves()
    {
        var project = TestData.Project();
        var employee = TestData.Employee();
        _projects.GetByIdAsync(1, _ct).Returns(project);
        _employees.GetByIdAsync(1, _ct).Returns(employee);

        await _sut.AssignEmployeeAsync(1, 1, _ct);

        Assert.Contains(project, employee.Projects);
        await _employees.Received(1).UpdateAsync(employee, _ct);
    }

    // ---- remove ----

    [Fact]
    public async Task Remove_throws_NotFound_when_project_missing()
    {
        _projects.GetByIdAsync(1, _ct).Returns((Project?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.RemoveEmployeeAsync(1, 1, _ct));
    }

    [Fact]
    public async Task Remove_throws_NotFound_when_employee_missing()
    {
        _projects.GetByIdAsync(1, _ct).Returns(TestData.Project());
        _employees.GetByIdAsync(1, _ct).Returns((Employee?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.RemoveEmployeeAsync(1, 1, _ct));
    }

    [Fact]
    public async Task Remove_throws_NotFound_when_not_assigned()
    {
        _projects.GetByIdAsync(1, _ct).Returns(TestData.Project());
        _employees.GetByIdAsync(1, _ct).Returns(TestData.Employee());

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _sut.RemoveEmployeeAsync(1, 1, _ct));
        Assert.Contains("not assigned", ex.Message);
    }

    [Fact]
    public async Task Remove_unassigns_and_saves()
    {
        var project = TestData.Project();
        var employee = TestData.Employee();
        employee.Projects.Add(project);
        _projects.GetByIdAsync(1, _ct).Returns(project);
        _employees.GetByIdAsync(1, _ct).Returns(employee);

        await _sut.RemoveEmployeeAsync(1, 1, _ct);

        Assert.Empty(employee.Projects);
        await _employees.Received(1).UpdateAsync(employee, _ct);
    }

    // ---- delete ----

    [Fact]
    public async Task Delete_returns_false_when_missing()
    {
        _projects.GetByIdAsync(9, _ct).Returns((Project?)null);

        Assert.False(await _sut.DeleteAsync(9, _ct));
    }

    [Fact]
    public async Task Delete_throws_Conflict_while_employees_are_assigned()
    {
        var project = TestData.Project();
        project.Employees.Add(TestData.Employee());
        _projects.GetByIdAsync(1, _ct).Returns(project);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(1, _ct));
        await _projects.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }

    [Fact]
    public async Task Delete_removes_an_empty_project()
    {
        var project = TestData.Project();
        _projects.GetByIdAsync(1, _ct).Returns(project);

        Assert.True(await _sut.DeleteAsync(1, _ct));
        await _projects.Received(1).DeleteAsync(project, _ct);
    }
}
