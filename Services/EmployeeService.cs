using EmployeeManagement.DTOs.Employees;
using EmployeeManagement.Models;
using EmployeeManagement.Repositories.Interfaces;
using EmployeeManagement.Services.Interfaces;

namespace EmployeeManagement.Services;

public class EmployeeService  : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository
    )
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task<List<EmployeeResponseDto>> GetAllAsync()
    {
        var employees = await _employeeRepository.GetAllAsync();

        return employees.Select(e => new EmployeeResponseDto
        {
            Id = e.Id,
            Name = e.Name,
            Email = e.Email,
            Salary = e.Salary,
            DepartmentName = e.Department.Name
        }).ToList();
    }

    public async Task<EmployeeResponseDto?> GetByIdAsync(int id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);

        if(employee is null)
            return null;
        
        return new EmployeeResponseDto
        {
            Id = employee.Id,
            Name = employee.Name,
            Email = employee.Email,
            Salary = employee.Salary,
            DepartmentName = employee.Department.Name
        };
    }

    public async Task<EmployeeResponseDto> CreateAsync(CreateEmployeeDto dto)
    {
        // Business validation : Department must exist
        var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId);

        if(department is null)
            throw new ArgumentException("Department doesn't exist !");
        
        // Business validation: Email must be unique
        var existingEmployee = await _employeeRepository.GetByEmailAsync(dto.Email);

        if(existingEmployee is not null)
            throw new InvalidOperationException("Email already exists !");
        
        var employee = new Employee
        {
            Name = dto.Name,
            Email = dto.Email,
            Salary = dto.Salary,
            DepartmentId = dto.DepartmentId
        };

        await _employeeRepository.AddAsync(employee);

        return new EmployeeResponseDto
        {
            Id = employee.Id,
            Name = employee.Name,
            Email = employee.Email,
            Salary = employee.Salary,
            DepartmentName = employee.Department.Name
        };
    }

    public async Task<bool> UpdateAsync(int id, UpdateEmployeeDto dto)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        if(employee is null)
            return false;
        
        var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId);
        if(department is null)
            throw new ArgumentException("Department doesn't exist !");

        var existingEmployee = await _employeeRepository.GetByEmailAsync(dto.Email);
        if(existingEmployee is not null && existingEmployee.Id != id)
            throw new InvalidOperationException("Email already exists !");
        
        employee.Name = dto.Name;
        employee.Email = dto.Email;
        employee.Salary = dto.Salary;
        employee.DepartmentId = dto.DepartmentId;
        await _employeeRepository.UpdateAsync(employee);

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        if(employee is null)
            return false;
        
        await _employeeRepository.DeleteAsync(employee);

        return true;
    }
}