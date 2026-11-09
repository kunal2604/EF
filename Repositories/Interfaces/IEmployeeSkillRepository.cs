using EmployeeManagement.Models;

namespace EmployeeManagement.Repositories.Interfaces;

public interface IEmployeeSkillRepository
{
    Task<List<EmployeeSkill>> GetByEmployeeIdAsync(int employeeId);
    Task<EmployeeSkill?> GetAsync(int employeeId, int skillId);
    Task AddAsync(EmployeeSkill employeeSkill);
    Task UpdateAsync(EmployeeSkill employeeSkill);

}