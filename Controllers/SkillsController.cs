using EmployeeManagement.DTOs.Common;


using EmployeeManagement.DTOs.EmployeeSkills;
using EmployeeManagement.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SkillsController : ControllerBase
{
    private readonly ISkillService _skillService;

    public SkillsController(ISkillService skillService)
    {
        _skillService = skillService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSkillDto dto, CancellationToken cancellationToken)
    {
        var id = await _skillService.CreateAsync(dto, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new { id, message = "Skill created successfully." });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQuery query,
        CancellationToken cancellationToken)
    {
        var skills = await _skillService.GetPagedAsync(query, cancellationToken);

        return Ok(skills);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var skill = await _skillService.GetByIdAsync(id, cancellationToken);

        if (skill is null)
            return NotFound();

        return Ok(skill);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _skillService.DeleteAsync(id, cancellationToken);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
