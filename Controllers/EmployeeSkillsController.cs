using EmployeeManagement.DTOs.EmployeeSkills;
using EmployeeManagement.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Controllers;

[ApiController]
[Route("api/employees/{employeeId:int}/skills")]
public class EmployeeSkillsController : ControllerBase
{
    private readonly IEmployeeSkillService _service;

    public EmployeeSkillsController(IEmployeeSkillService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> AssignSkill(
        int employeeId,
        AssignSkillDto dto,
        CancellationToken cancellationToken)
    {
        await _service.AssignSkillAsync(employeeId, dto, cancellationToken);

        return Ok("Skill assigned successfully.");
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployeeSkills(int employeeId, CancellationToken cancellationToken)
    {
        var skills = await _service.GetEmployeeSkillsAsync(employeeId, cancellationToken);

        return Ok(skills);
    }

    [HttpPut("{skillId:int}")]
    public async Task<IActionResult> UpdateSkill(
        int employeeId,
        int skillId,
        UpdateEmployeeSkillDto dto,
        CancellationToken cancellationToken)
    {
        await _service.UpdateSkillAsync(employeeId, skillId, dto, cancellationToken);

        return NoContent();
    }
}