using EmployeeManagement.DTOs.Common;
using EmployeeManagement.DTOs.EmployeeSkills;

namespace EmployeeManagement.Services.Interfaces;
public interface ISkillService
{
    Task<int> CreateAsync(CreateSkillDto dto, CancellationToken cancellationToken);

    Task<PagedResult<SkillResponseDto>> GetPagedAsync(PagedQuery query, CancellationToken cancellationToken);

    Task<SkillResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
}