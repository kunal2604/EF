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

    public async Task<int> CreateAsync(CreateSkillDto dto)
    {
        var existingSkills = await _skillRepository.GetAllAsync();

        if (existingSkills.Any(s =>
            s.Name.Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException("Skill already exists.");
        }

        var skill = new Skill
        {
            Name = dto.Name.Trim()
        };

        await _skillRepository.AddAsync(skill);

        return skill.Id;
    }

    public async Task<List<SkillResponseDto>> GetAllAsync()
    {
        var skills = await _skillRepository.GetAllAsync();

        return skills.Select(s => new SkillResponseDto
        {
            Id = s.Id,
            Name = s.Name
        }).ToList();
    }

    public async Task<SkillResponseDto?> GetByIdAsync(int id)
    {
        var skill = await _skillRepository.GetByIdAsync(id);

        if (skill is null)
            return null;

        return new SkillResponseDto
        {
            Id = skill.Id,
            Name = skill.Name
        };
    }
}