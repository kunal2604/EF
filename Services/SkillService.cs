using EmployeeManagement.Mappings;
using EmployeeManagement.DTOs.Common;
using EmployeeManagement.Exceptions;
using EmployeeManagement.DTOs.EmployeeSkills;
using EmployeeManagement.Models;
using EmployeeManagement.Repositories.Interfaces;
using EmployeeManagement.Services.Interfaces;

namespace EmployeeManagement.Services;

public class SkillService : ISkillService
{
    private readonly ISkillRepository _skillRepository;

    public SkillService(ISkillRepository skillRepository)
    {
        _skillRepository = skillRepository;
    }

    public async Task<SkillResponseDto> CreateAsync(CreateSkillDto dto, CancellationToken cancellationToken)
    {
        if (await _skillRepository.ExistsByNameAsync(dto.Name.Trim(), cancellationToken))
            throw new ConflictException("Skill already exists.");

        var skill = new Skill
        {
            Name = dto.Name.Trim()
        };

        await _skillRepository.AddAsync(skill, cancellationToken);

        return skill.ToResponseDto();
    }

    public async Task<PagedResult<SkillResponseDto>> GetPagedAsync(
        PagedQuery query, CancellationToken cancellationToken)
    {
        var (skills, totalCount) = await _skillRepository.GetPagedAsync(
            query.Page, query.PageSize, cancellationToken);

        var items = skills.Select(s => s.ToResponseDto()).ToList();

        return PagedResult<SkillResponseDto>.Create(items, totalCount, query);
    }

    public async Task<SkillResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var skill = await _skillRepository.GetByIdAsync(id, cancellationToken);

        if (skill is null)
            return null;

        return skill.ToResponseDto();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var skill = await _skillRepository.GetByIdAsync(id, cancellationToken);

        if (skill is null)
            return false;

        // If employees still have this skill, the foreign key rejects the delete;
        // GlobalExceptionHandler turns that into a 409.
        await _skillRepository.DeleteAsync(skill, cancellationToken);

        return true;
    }
}
