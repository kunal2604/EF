using EmployeeManagement.Models;

namespace EmployeeManagement.Repositories.Interfaces;

public interface ISkillRepository
{
    Task<List<Skill>> GetAllAsync(CancellationToken cancellationToken);
    Task<Skill?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task AddAsync(Skill skill, CancellationToken cancellationToken);
}
