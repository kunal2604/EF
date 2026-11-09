using EmployeeManagement.DTOs.Departments;

namespace EmployeeManagement.Services.Interfaces;

public interface IDepartmentService
{
    Task<List<DepartmentResponseDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<DepartmentResponseDto> CreateAsync(CreateDepartmentDto dto, CancellationToken cancellationToken);
}