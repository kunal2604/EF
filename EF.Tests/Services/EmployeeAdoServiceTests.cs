using EmployeeManagement.Models;
using EmployeeManagement.Repositories.AdoNet.Interfaces;
using EmployeeManagement.Services;
using NSubstitute;

namespace EF.Tests.Services;

public class EmployeeAdoServiceTests
{
    private readonly IEmployeeAdoRepository _repo = Substitute.For<IEmployeeAdoRepository>();
    private readonly EmployeeAdoService _sut;

    public EmployeeAdoServiceTests() => _sut = new EmployeeAdoService(_repo);

    [Fact]
    public async Task GetAll_returns_what_the_repository_returns()
    {
        var rows = new List<Employee> { TestData.Employee(1), TestData.Employee(2, "b@test.com") };
        _repo.GetAllEmployeesAsync().Returns(rows);

        var result = await _sut.GetAllEmployeesAsync();

        Assert.Same(rows, result);
    }

    [Fact]
    public async Task GetAll_returns_an_empty_list_when_there_are_no_employees()
    {
        _repo.GetAllEmployeesAsync().Returns(new List<Employee>());

        Assert.Empty(await _sut.GetAllEmployeesAsync());
    }

    [Fact]
    public async Task GetById_returns_the_employee_for_that_id()
    {
        var employee = TestData.Employee(7);
        _repo.GetEmployeeByIdAsync(7).Returns(employee);

        Assert.Same(employee, await _sut.GetEmployeeByIdAsync(7));
        await _repo.DidNotReceive().GetEmployeeByIdAsync(Arg.Is<int>(id => id != 7));
    }

    [Fact]
    public async Task GetById_returns_null_when_the_employee_does_not_exist()
    {
        _repo.GetEmployeeByIdAsync(9).Returns((Employee?)null);

        Assert.Null(await _sut.GetEmployeeByIdAsync(9));
    }
}
