using EmployeeManagement.Exceptions;
using EmployeeManagement.DTOs.EmployeeSkills;
using EmployeeManagement.Models;
using EmployeeManagement.Repositories.Interfaces;
using EmployeeManagement.Services.Interfaces;

namespace EmployeeManagement.Services;

public class EmployeeSkillService : IEmployeeSkillService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly IEmployeeSkillRepository _employeeSkillRepository;

    public EmployeeSkillService(
        IEmployeeRepository employeeRepository,
        ISkillRepository skillRepository,
        IEmployeeSkillRepository employeeSkillRepository)
    {
        _employeeRepository = employeeRepository;
        _skillRepository = skillRepository;
        _employeeSkillRepository = employeeSkillRepository;
    }

    public async Task AssignSkillAsync(int employeeId, AssignSkillDto dto, CancellationToken cancellationToken)
    {
        if (!await _employeeRepository.ExistsAsync(employeeId, cancellationToken))
            throw new NotFoundException("Employee not found.");

        var skill = await _skillRepository.GetByIdAsync(dto.SkillId, cancellationToken);

        if (skill is null)
            throw new NotFoundException("Skill not found.");

        var existing = await _employeeSkillRepository.GetAsync(
            employeeId, dto.SkillId, cancellationToken);

        if (existing is not null)
            throw new ConflictException(
                "This skill is already assigned to the employee.");

        var employeeSkill = new EmployeeSkill
        {
            EmployeeId = employeeId,
            SkillId = dto.SkillId,
            ProficiencyLevel = dto.ProficiencyLevel,
            YearsOfExperience = dto.YearsOfExperience
        };

        await _employeeSkillRepository.AddAsync(employeeSkill, cancellationToken);
    }

    public async Task<List<EmployeeSkillResponseDto>> GetEmployeeSkillsAsync(
        int employeeId,
        CancellationToken cancellationToken)
    {
        if (!await _employeeRepository.ExistsAsync(employeeId, cancellationToken))
            throw new NotFoundException("Employee not found.");

        return await _employeeSkillRepository.GetByEmployeeIdAsync(employeeId, cancellationToken);
    }

    public async Task UpdateSkillAsync(
        int employeeId,
        int skillId,
        UpdateEmployeeSkillDto dto,
        CancellationToken cancellationToken)
    {
        var employeeSkill = await _employeeSkillRepository.GetAsync(
            employeeId, skillId, cancellationToken);

        if (employeeSkill is null)
            throw new NotFoundException(
                "Employee skill assignment not found.");

        employeeSkill.ProficiencyLevel = dto.ProficiencyLevel;
        employeeSkill.YearsOfExperience = dto.YearsOfExperience;

        await _employeeSkillRepository.UpdateAsync(employeeSkill, cancellationToken);
    }

    public async Task RemoveSkillAsync(
        int employeeId,
        int skillId,
        CancellationToken cancellationToken)
    {
        var employeeSkill = await _employeeSkillRepository.GetAsync(
            employeeId, skillId, cancellationToken);

        if (employeeSkill is null)
            throw new NotFoundException(
                "Employee skill assignment not found.");

        await _employeeSkillRepository.DeleteAsync(employeeSkill, cancellationToken);
    }
}
