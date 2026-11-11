using EmployeeManagement.DTOs.Employees;
using EmployeeManagement.Models;

namespace EmployeeManagement.Repositories.Interfaces;

public interface IEmployeeRepository
{
    /// <summary>Read-only page, projected straight into DTOs.</summary>
    Task<(List<EmployeeResponseDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken);
    /// <summary>Read-only single employee as a DTO; null if not found. Use <see cref="GetByIdAsync"/> when you need to change it.</summary>
    Task<EmployeeResponseDto?> GetResponseByIdAsync(int id, CancellationToken cancellationToken);
    Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task AddAsync(Employee employee, CancellationToken cancellationToken);
    Task UpdateAsync(Employee employee, CancellationToken cancellationToken);
    Task DeleteAsync(Employee employee, CancellationToken cancellationToken);
}