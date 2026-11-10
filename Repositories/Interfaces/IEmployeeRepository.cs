using EmployeeManagement.Models;

namespace EmployeeManagement.Repositories.Interfaces;

public interface IEmployeeRepository
{
    Task<(List<Employee> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken);
    Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task AddAsync(Employee employee, CancellationToken cancellationToken);
    Task UpdateAsync(Employee employee, CancellationToken cancellationToken);
    Task DeleteAsync(Employee employee, CancellationToken cancellationToken);
}