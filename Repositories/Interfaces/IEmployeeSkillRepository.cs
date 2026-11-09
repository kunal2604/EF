using EmployeeManagement.Models;

namespace EmployeeManagement.Repositories.Interfaces;

public interface IEmployeeSkillRepository
{
    Task<List<EmployeeSkill>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken);
    Task<EmployeeSkill?> GetAsync(int employeeId, int skillId, CancellationToken cancellationToken);
    Task AddAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken);
    Task UpdateAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken);

}