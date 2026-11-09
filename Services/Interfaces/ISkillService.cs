using EmployeeManagement.DTOs.EmployeeSkills;

namespace EmployeeManagement.Services.Interfaces;
public interface ISkillService
{
    Task<int> CreateAsync(CreateSkillDto dto, CancellationToken cancellationToken);

    Task<List<SkillResponseDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<SkillResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
}