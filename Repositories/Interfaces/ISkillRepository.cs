using EmployeeManagement.Models;

namespace EmployeeManagement.Repositories.Interfaces;

public interface ISkillRepository
{
    Task<(List<Skill> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken);
    Task<Skill?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);
    Task AddAsync(Skill skill, CancellationToken cancellationToken);
}
