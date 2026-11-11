using EmployeeManagement.Mappings;
using EmployeeManagement.DTOs.Common;
using EmployeeManagement.DTOs.Employees;
using EmployeeManagement.Exceptions;
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

    public async Task<PagedResult<EmployeeResponseDto>> GetPagedAsync(
        PagedQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _employeeRepository.GetPagedAsync(
            query.Page, query.PageSize, cancellationToken);

        return PagedResult<EmployeeResponseDto>.Create(items, totalCount, query);
    }

    public async Task<EmployeeResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _employeeRepository.GetResponseByIdAsync(id, cancellationToken);
    }

    public async Task<EmployeeResponseDto> CreateAsync(CreateEmployeeDto dto, CancellationToken cancellationToken)
    {
        // Business validation : Department must exist
        var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId, cancellationToken);

        if(department is null)
            throw new BadRequestException("Department doesn't exist !");
        
        // Business validation: Email must be unique
        var existingEmployee = await _employeeRepository.GetByEmailAsync(dto.Email, cancellationToken);

        if(existingEmployee is not null)
            throw new ConflictException("Email already exists !");
        
        var employee = new Employee
        {
            Name = dto.Name,
            Email = dto.Email,
            Salary = dto.Salary,
            JoiningDate = dto.JoiningDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            DepartmentId = dto.DepartmentId,
            Department = department
        };

        await _employeeRepository.AddAsync(employee, cancellationToken);

        return employee.ToResponseDto();
    }

    public async Task<bool> UpdateAsync(int id, UpdateEmployeeDto dto, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(id, cancellationToken);
        if(employee is null)
            return false;
        
        var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId, cancellationToken);
        if(department is null)
            throw new BadRequestException("Department doesn't exist !");

        var existingEmployee = await _employeeRepository.GetByEmailAsync(dto.Email, cancellationToken);
        if(existingEmployee is not null && existingEmployee.Id != id)
            throw new ConflictException("Email already exists !");
        
        employee.Name = dto.Name;
        employee.Email = dto.Email;
        employee.Salary = dto.Salary;
        employee.JoiningDate = dto.JoiningDate!.Value; // guaranteed by [Required] on the DTO
        employee.DepartmentId = dto.DepartmentId;
        await _employeeRepository.UpdateAsync(employee, cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(id, cancellationToken);
        if(employee is null)
            return false;
        
        // GetByIdAsync already loaded Projects; EF would otherwise throw while tracking
        // the join rows, before the database ever sees the delete.
        if(employee.Projects.Count > 0)
            throw new ConflictException(
                "This employee can't be deleted while they are assigned to projects. Remove them from the projects first.");

        await _employeeRepository.DeleteAsync(employee, cancellationToken);

        return true;
    }
}