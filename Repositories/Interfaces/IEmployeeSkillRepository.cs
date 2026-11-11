using EmployeeManagement.DTOs.EmployeeSkills;
using EmployeeManagement.Models;

namespace EmployeeManagement.Repositories.Interfaces;

public interface IEmployeeSkillRepository
{
    /// <summary>Read-only list, projected straight into DTOs.</summary>
    Task<List<EmployeeSkillResponseDto>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken);
    Task<EmployeeSkill?> GetAsync(int employeeId, int skillId, CancellationToken cancellationToken);
    Task AddAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken);
    Task UpdateAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken);
    Task DeleteAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken);

}