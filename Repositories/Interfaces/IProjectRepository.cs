using EmployeeManagement.Models;

namespace EmployeeManagement.Repositories.Interfaces;

public interface IProjectRepository
{
    Task<List<Project>> GetAllAsync(CancellationToken cancellationToken);
    Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task AddAsync(Project project, CancellationToken cancellationToken);
}