using EmployeeManagement.DTOs.EmployeeSkills;

namespace EmployeeManagement.Services.Interfaces;

public interface IEmployeeSkillService
{
    Task AssignSkillAsync(int employeeId, AssignSkillDto dto, CancellationToken cancellationToken);

    Task<List<EmployeeSkillResponseDto>> GetEmployeeSkillsAsync(
        int employeeId,
        CancellationToken cancellationToken);

    Task UpdateSkillAsync(
        int employeeId,
        int skillId,
        UpdateEmployeeSkillDto dto,
        CancellationToken cancellationToken);

    Task RemoveSkillAsync(
        int employeeId,
        int skillId,
        CancellationToken cancellationToken);
}