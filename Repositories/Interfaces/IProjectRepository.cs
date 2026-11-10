using EmployeeManagement.Models;

namespace EmployeeManagement.Repositories.Interfaces;

public interface IProjectRepository
{
    Task<(List<Project> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken);
    Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task AddAsync(Project project, CancellationToken cancellationToken);
    Task DeleteAsync(Project project, CancellationToken cancellationToken);
}