using EmployeeManagement.DTOs.Departments;
using EmployeeManagement.Models;
using EmployeeManagement.Repositories.Interfaces;
using EmployeeManagement.Services.Interfaces;

namespace EmployeeManagement.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;

    public DepartmentService(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<List<DepartmentResponseDto>> GetAllAsync()
    {
        var departments = await _departmentRepository.GetAllAsync();

        return departments.Select(d => new DepartmentResponseDto
        {
            Id = d.Id,
            Name = d.Name
        }).ToList();
    }

    public async Task<DepartmentResponseDto> CreateAsync(CreateDepartmentDto dto)
    {
        var department = new Department
        {
            Name = dto.Name
        };

        await _departmentRepository.AddAsync(department);

        return new DepartmentResponseDto
        {
            Id = department.Id,
            Name = department.Name
        };        
    }
}