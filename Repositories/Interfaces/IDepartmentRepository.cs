using EmployeeManagement.Models;

namespace EmployeeManagement.Repositories.Interfaces;

public interface IDepartmentRepository
{
    Task<List<Department>> GetAllAsync(CancellationToken cancellationToken);
    Task<Department?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task AddAsync(Department department, CancellationToken cancellationToken);
}