using EmployeeManagement.DTOs.Projects;
using EmployeeManagement.Models;

namespace EmployeeManagement.Repositories.Interfaces;

public interface IProjectRepository
{
    /// <summary>Read-only page, projected straight into DTOs.</summary>
    Task<(List<ProjectResponseDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken);
    /// <summary>Read-only single project as a DTO; null if not found. Use <see cref="GetByIdAsync"/> when you need to change it.</summary>
    Task<ProjectResponseDto?> GetResponseByIdAsync(int id, CancellationToken cancellationToken);
    Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task AddAsync(Project project, CancellationToken cancellationToken);
    Task DeleteAsync(Project project, CancellationToken cancellationToken);
}