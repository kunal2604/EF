using EmployeeManagement.DTOs.Common;
using EmployeeManagement.DTOs.Departments;

namespace EmployeeManagement.Services.Interfaces;

public interface IDepartmentService
{
    Task<PagedResult<DepartmentResponseDto>> GetPagedAsync(PagedQuery query, CancellationToken cancellationToken);

    Task<DepartmentResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<DepartmentResponseDto> CreateAsync(CreateDepartmentDto dto, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, UpdateDepartmentDto dto, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}