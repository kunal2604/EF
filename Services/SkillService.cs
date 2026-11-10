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

    public async Task<int> CreateAsync(CreateSkillDto dto, CancellationToken cancellationToken)
    {
        if (await _skillRepository.ExistsByNameAsync(dto.Name.Trim(), cancellationToken))
            throw new ConflictException("Skill already exists.");

        var skill = new Skill
        {
            Name = dto.Name.Trim()
        };

        await _skillRepository.AddAsync(skill, cancellationToken);

        return skill.Id;
    }

    public async Task<PagedResult<SkillResponseDto>> GetPagedAsync(
        PagedQuery query, CancellationToken cancellationToken)
    {
        var (skills, totalCount) = await _skillRepository.GetPagedAsync(
            query.Page, query.PageSize, cancellationToken);

        var items = skills.Select(s => new SkillResponseDto
        {
            Id = s.Id,
            Name = s.Name
        }).ToList();

        return PagedResult<SkillResponseDto>.Create(items, totalCount, query);
    }

    public async Task<SkillResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var skill = await _skillRepository.GetByIdAsync(id, cancellationToken);

        if (skill is null)
            return null;

        return new SkillResponseDto
        {
            Id = skill.Id,
            Name = skill.Name
        };
    }
}