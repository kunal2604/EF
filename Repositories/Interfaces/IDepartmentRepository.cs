using EmployeeManagement.Models;

namespace EmployeeManagement.Repositories.Interfaces;

public interface IDepartmentRepository
{
    Task<(List<Department> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken);
    Task<Department?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task AddAsync(Department department, CancellationToken cancellationToken);
    Task UpdateAsync(Department department, CancellationToken cancellationToken);
    Task DeleteAsync(Department department, CancellationToken cancellationToken);
}