using EmployeeManagement.DTOs.Common;
using EmployeeManagement.DTOs.Departments;

namespace EmployeeManagement.Services.Interfaces;

public interface IDepartmentService
{
    Task<PagedResult<DepartmentResponseDto>> GetPagedAsync(PagedQuery query, CancellationToken cancellationToken);

    Task<DepartmentResponseDto> CreateAsync(CreateDepartmentDto dto, CancellationToken cancellationToken);
}