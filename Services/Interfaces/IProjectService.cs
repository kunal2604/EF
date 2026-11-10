using EmployeeManagement.DTOs.Common;
using EmployeeManagement.DTOs.Projects;

namespace EmployeeManagement.Services.Interfaces;

public interface IProjectService
{
    Task<PagedResult<ProjectResponseDto>> GetPagedAsync(PagedQuery query, CancellationToken cancellationToken);
    Task<ProjectResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<ProjectResponseDto> CreateAsync(CreateProjectDto dto, CancellationToken cancellationToken);
    Task AssignEmployeeAsync(int projectId, int employeeId, CancellationToken cancellationToken);
}