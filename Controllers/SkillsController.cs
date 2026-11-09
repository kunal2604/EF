

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
    public async Task<IActionResult> Create(CreateSkillDto dto)
    {
        var id = await _skillService.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new { id, message = "Skill created successfully." });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var skills = await _skillService.GetAllAsync();

        return Ok(skills);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var skill = await _skillService.GetByIdAsync(id);

        if (skill is null)
            return NotFound();

        return Ok(skill);
    }
}