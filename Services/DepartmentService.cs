using EmployeeManagement.DTOs.Common;
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

    public async Task<PagedResult<DepartmentResponseDto>> GetPagedAsync(
        PagedQuery query, CancellationToken cancellationToken)
    {
        var (departments, totalCount) = await _departmentRepository.GetPagedAsync(
            query.Page, query.PageSize, cancellationToken);

        var items = departments.Select(d => new DepartmentResponseDto
        {
            Id = d.Id,
            Name = d.Name
        }).ToList();

        return PagedResult<DepartmentResponseDto>.Create(items, totalCount, query);
    }

    public async Task<DepartmentResponseDto> CreateAsync(CreateDepartmentDto dto, CancellationToken cancellationToken)
    {
        var department = new Department
        {
            Name = dto.Name
        };

        await _departmentRepository.AddAsync(department, cancellationToken);

        return new DepartmentResponseDto
        {
            Id = department.Id,
            Name = department.Name
        };        
    }

    public async Task<bool> UpdateAsync(int id, UpdateDepartmentDto dto, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(id, cancellationToken);

        if (department is null)
            return false;

        department.Name = dto.Name;

        await _departmentRepository.UpdateAsync(department, cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(id, cancellationToken);

        if (department is null)
            return false;

        // If employees still belong to this department, the foreign key rejects the
        // delete; GlobalExceptionHandler turns that into a 409.
        await _departmentRepository.DeleteAsync(department, cancellationToken);

        return true;
    }
}
