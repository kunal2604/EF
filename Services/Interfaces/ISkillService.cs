using EmployeeManagement.DTOs.EmployeeSkills;

namespace EmployeeManagement.Services.Interfaces;
public interface ISkillService
{
    Task<int> CreateAsync(CreateSkillDto dto);

    Task<List<SkillResponseDto>> GetAllAsync();

    Task<SkillResponseDto?> GetByIdAsync(int id);
}