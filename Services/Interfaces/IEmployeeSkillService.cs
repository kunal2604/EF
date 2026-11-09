using EmployeeManagement.DTOs.EmployeeSkills;

namespace EmployeeManagement.Services.Interfaces;

public interface IEmployeeSkillService
{
    Task AssignSkillAsync(int employeeId, AssignSkillDto dto);

    Task<List<EmployeeSkillResponseDto>> GetEmployeeSkillsAsync(
        int employeeId);

    Task UpdateSkillAsync(
        int employeeId,
        int skillId,
        UpdateEmployeeSkillDto dto);
}