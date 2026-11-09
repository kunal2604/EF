using EmployeeManagement.Models;

namespace EmployeeManagement.Repositories.Interfaces;

public interface ISkillRepository
{
    Task<List<Skill>> GetAllAsync();
    Task<Skill?> GetByIdAsync(int id);
    Task AddAsync(Skill skill);
}
