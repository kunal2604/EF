using EmployeeManagement.DTOs.Common;
using EmployeeManagement.DTOs.Employees;
using EmployeeManagement.Exceptions;
using EmployeeManagement.Mappings;
using EmployeeManagement.Models;
using EmployeeManagement.Repositories.Interfaces;
using EmployeeManagement.Services;
using NSubstitute;

namespace EF.Tests.Services;

public class EmployeeServiceTests
{
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly IDepartmentRepository _departments = Substitute.For<IDepartmentRepository>();
    private readonly EmployeeService _sut;
    private readonly CancellationToken _ct = new CancellationTokenSource().Token;

    public EmployeeServiceTests() => _sut = new EmployeeService(_employees, _departments);

    private static CreateEmployeeDto NewDto(DateOnly? joined = null) => new()
    {
        Name = "Asha", Email = "asha@test.com", Salary = 50000m, JoiningDate = joined, DepartmentId = 1
    };

    private static UpdateEmployeeDto UpdateDto(string email = "asha@test.com") => new()
    {
        Name = "New Name", Email = email, Salary = 60000m,
        JoiningDate = new DateOnly(2021, 3, 4), DepartmentId = 2
    };

    // ---- paging ----

    [Fact]
    public async Task GetPaged_passes_page_to_repository_and_builds_envelope()
    {
        var rows = new List<EmployeeResponseDto> { TestData.Employee(1).ToResponseDto(), TestData.Employee(2, "b@test.com").ToResponseDto() };
        _employees.GetPagedAsync(2, 2, _ct).Returns((rows, 5));

        var result = await _sut.GetPagedAsync(new PagedQuery { Page = 2, PageSize = 2 }, _ct);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Engineering", result.Items[0].DepartmentName);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages); // ceil(5 / 2)
    }

    [Fact]
    public async Task GetPaged_with_no_rows_reports_zero_pages()
    {
        _employees.GetPagedAsync(1, 20, _ct).Returns((new List<EmployeeResponseDto>(), 0));

        var result = await _sut.GetPagedAsync(new PagedQuery(), _ct);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalPages);
    }

    // ---- get by id ----

    [Fact]
    public async Task GetById_returns_null_when_missing()
    {
        _employees.GetResponseByIdAsync(9, _ct).Returns((EmployeeResponseDto?)null);

        Assert.Null(await _sut.GetByIdAsync(9, _ct));
    }

    [Fact]
    public async Task GetById_maps_the_employee()
    {
        _employees.GetResponseByIdAsync(1, _ct).Returns(TestData.Employee().ToResponseDto());

        var dto = await _sut.GetByIdAsync(1, _ct);

        Assert.NotNull(dto);
        Assert.Equal("asha@test.com", dto.Email);
        Assert.Equal("Engineering", dto.DepartmentName);
    }

    // ---- create ----

    [Fact]
    public async Task Create_throws_BadRequest_when_department_missing()
    {
        _departments.GetByIdAsync(1, _ct).Returns((Department?)null);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateAsync(NewDto(), _ct));

        Assert.Equal(400, ex.StatusCode);
        await _employees.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Create_throws_Conflict_when_email_exists()
    {
        _departments.GetByIdAsync(1, _ct).Returns(TestData.Department());
        _employees.GetByEmailAsync("asha@test.com", _ct).Returns(TestData.Employee());

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(NewDto(), _ct));

        Assert.Equal(409, ex.StatusCode);
        await _employees.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Create_saves_the_employee_and_returns_the_department_name()
    {
        _departments.GetByIdAsync(1, _ct).Returns(TestData.Department(1, "Platform"));
        _employees.GetByEmailAsync(Arg.Any<string>(), _ct).Returns((Employee?)null);

        var dto = await _sut.CreateAsync(NewDto(new DateOnly(2020, 9, 1)), _ct);

        Assert.Equal("Platform", dto.DepartmentName);
        Assert.Equal(new DateOnly(2020, 9, 1), dto.JoiningDate);
        await _employees.Received(1).AddAsync(
            Arg.Is<Employee>(e => e.Email == "asha@test.com" && e.DepartmentId == 1), _ct);
    }

    [Fact]
    public async Task Create_defaults_JoiningDate_to_today_when_omitted()
    {
        _departments.GetByIdAsync(1, _ct).Returns(TestData.Department());
        _employees.GetByEmailAsync(Arg.Any<string>(), _ct).Returns((Employee?)null);

        var before = DateOnly.FromDateTime(DateTime.UtcNow);
        var dto = await _sut.CreateAsync(NewDto(joined: null), _ct);
        var after = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.InRange(dto.JoiningDate, before, after);
        Assert.NotEqual(default, dto.JoiningDate); // never 0001-01-01
    }

    // ---- update ----

    [Fact]
    public async Task Update_returns_false_when_employee_missing()
    {
        _employees.GetByIdAsync(9, _ct).Returns((Employee?)null);

        Assert.False(await _sut.UpdateAsync(9, UpdateDto(), _ct));
    }

    [Fact]
    public async Task Update_throws_BadRequest_when_department_missing()
    {
        _employees.GetByIdAsync(1, _ct).Returns(TestData.Employee());
        _departments.GetByIdAsync(2, _ct).Returns((Department?)null);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.UpdateAsync(1, UpdateDto(), _ct));
    }

    [Fact]
    public async Task Update_throws_Conflict_when_email_belongs_to_someone_else()
    {
        _employees.GetByIdAsync(1, _ct).Returns(TestData.Employee(1));
        _departments.GetByIdAsync(2, _ct).Returns(TestData.Department(2));
        _employees.GetByEmailAsync("taken@test.com", _ct).Returns(TestData.Employee(7, "taken@test.com"));

        await Assert.ThrowsAsync<ConflictException>(() => _sut.UpdateAsync(1, UpdateDto("taken@test.com"), _ct));
        await _employees.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task Update_allows_keeping_your_own_email()
    {
        var employee = TestData.Employee(1);
        _employees.GetByIdAsync(1, _ct).Returns(employee);
        _departments.GetByIdAsync(2, _ct).Returns(TestData.Department(2));
        _employees.GetByEmailAsync("asha@test.com", _ct).Returns(employee); // same row

        Assert.True(await _sut.UpdateAsync(1, UpdateDto("asha@test.com"), _ct));
    }

    [Fact]
    public async Task Update_applies_every_field()
    {
        var employee = TestData.Employee(1);
        _employees.GetByIdAsync(1, _ct).Returns(employee);
        _departments.GetByIdAsync(2, _ct).Returns(TestData.Department(2));
        _employees.GetByEmailAsync(Arg.Any<string>(), _ct).Returns((Employee?)null);

        await _sut.UpdateAsync(1, UpdateDto("new@test.com"), _ct);

        Assert.Equal("New Name", employee.Name);
        Assert.Equal("new@test.com", employee.Email);
        Assert.Equal(60000m, employee.Salary);
        Assert.Equal(new DateOnly(2021, 3, 4), employee.JoiningDate);
        Assert.Equal(2, employee.DepartmentId);
        await _employees.Received(1).UpdateAsync(employee, _ct);
    }

    // ---- delete ----

    [Fact]
    public async Task Delete_returns_false_when_missing()
    {
        _employees.GetByIdAsync(9, _ct).Returns((Employee?)null);

        Assert.False(await _sut.DeleteAsync(9, _ct));
    }

    [Fact]
    public async Task Delete_throws_Conflict_while_assigned_to_projects()
    {
        var employee = TestData.Employee();
        employee.Projects.Add(TestData.Project());
        _employees.GetByIdAsync(1, _ct).Returns(employee);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(1, _ct));

        Assert.Contains("projects", ex.Message);
        await _employees.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }

    [Fact]
    public async Task Delete_removes_an_unassigned_employee()
    {
        var employee = TestData.Employee();
        _employees.GetByIdAsync(1, _ct).Returns(employee);

        Assert.True(await _sut.DeleteAsync(1, _ct));
        await _employees.Received(1).DeleteAsync(employee, _ct);
    }

    // ---- cancellation (regression for CancellationToken propagation) ----

    [Fact]
    public async Task The_cancellation_token_reaches_the_repositories()
    {
        _employees.GetByIdAsync(1, _ct).Returns(TestData.Employee());

        await _sut.DeleteAsync(1, _ct);

        // Substitutes were set up for _ct only; a different token would have returned null
        // (=> "false"), so reaching DeleteAsync proves the same token was passed through.
        await _employees.Received(1).DeleteAsync(Arg.Any<Employee>(), _ct);
    }
}
