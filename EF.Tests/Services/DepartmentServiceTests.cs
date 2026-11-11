using EmployeeManagement.DTOs.Common;
using EmployeeManagement.DTOs.Departments;
using EmployeeManagement.Exceptions;
using EmployeeManagement.Models;
using EmployeeManagement.Repositories.Interfaces;
using EmployeeManagement.Services;
using NSubstitute;

namespace EF.Tests.Services;

public class DepartmentServiceTests
{
    private readonly IDepartmentRepository _repo = Substitute.For<IDepartmentRepository>();
    private readonly DepartmentService _sut;
    private readonly CancellationToken _ct = new CancellationTokenSource().Token;

    public DepartmentServiceTests() => _sut = new DepartmentService(_repo);

    [Fact]
    public async Task GetPaged_builds_the_envelope()
    {
        _repo.GetPagedAsync(1, 20, _ct).Returns((new List<Department> { TestData.Department(), TestData.Department(2, "HR") }, 2));

        var result = await _sut.GetPagedAsync(new PagedQuery(), _ct);

        Assert.Equal(new[] { "Engineering", "HR" }, result.Items.Select(d => d.Name));
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetById_returns_null_when_missing()
    {
        _repo.GetByIdAsync(9, _ct).Returns((Department?)null);

        Assert.Null(await _sut.GetByIdAsync(9, _ct));
    }

    [Fact]
    public async Task GetById_maps_the_department()
    {
        _repo.GetByIdAsync(1, _ct).Returns(TestData.Department(1, "Platform"));

        var dto = await _sut.GetByIdAsync(1, _ct);

        Assert.Equal("Platform", dto!.Name);
    }

    [Fact]
    public async Task Create_saves_and_returns_the_department()
    {
        var dto = await _sut.CreateAsync(new CreateDepartmentDto { Name = "Legal" }, _ct);

        Assert.Equal("Legal", dto.Name);
        await _repo.Received(1).AddAsync(Arg.Is<Department>(d => d.Name == "Legal"), _ct);
    }

    [Fact]
    public async Task Create_throws_Conflict_when_the_name_is_taken()
    {
        _repo.NameExistsAsync("Legal", null, _ct).Returns(true);

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => _sut.CreateAsync(new CreateDepartmentDto { Name = "Legal" }, _ct));

        Assert.Equal("Department already exists !", ex.Message);
        await _repo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Update_throws_Conflict_when_another_department_has_the_name()
    {
        _repo.GetByIdAsync(1, _ct).Returns(TestData.Department());
        _repo.NameExistsAsync("HR", 1, _ct).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.UpdateAsync(1, new UpdateDepartmentDto { Name = "HR" }, _ct));

        await _repo.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task Update_excludes_the_department_itself_from_the_name_check()
    {
        var department = TestData.Department();
        _repo.GetByIdAsync(1, _ct).Returns(department);

        await _sut.UpdateAsync(1, new UpdateDepartmentDto { Name = "Engineering" }, _ct); // same name, own row

        await _repo.Received(1).NameExistsAsync("Engineering", 1, _ct);
        await _repo.Received(1).UpdateAsync(department, _ct);
    }

    [Fact]
    public async Task Update_returns_false_when_missing()
    {
        _repo.GetByIdAsync(9, _ct).Returns((Department?)null);

        Assert.False(await _sut.UpdateAsync(9, new UpdateDepartmentDto { Name = "x" }, _ct));
        await _repo.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task Update_renames_the_department()
    {
        var department = TestData.Department();
        _repo.GetByIdAsync(1, _ct).Returns(department);

        Assert.True(await _sut.UpdateAsync(1, new UpdateDepartmentDto { Name = "Renamed" }, _ct));
        Assert.Equal("Renamed", department.Name);
        await _repo.Received(1).UpdateAsync(department, _ct);
    }

    [Fact]
    public async Task Delete_returns_false_when_missing()
    {
        _repo.GetByIdAsync(9, _ct).Returns((Department?)null);

        Assert.False(await _sut.DeleteAsync(9, _ct));
    }

    [Fact]
    public async Task Delete_removes_the_department()
    {
        var department = TestData.Department();
        _repo.GetByIdAsync(1, _ct).Returns(department);

        Assert.True(await _sut.DeleteAsync(1, _ct));
        await _repo.Received(1).DeleteAsync(department, _ct);
    }
}
