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
        AssignSkillDto dto)
    {
        await _service.AssignSkillAsync(employeeId, dto);

        return Ok("Skill assigned successfully.");
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployeeSkills(int employeeId)
    {
        var skills = await _service.GetEmployeeSkillsAsync(employeeId);

        return Ok(skills);
    }

    [HttpPut("{skillId:int}")]
    public async Task<IActionResult> UpdateSkill(
        int employeeId,
        int skillId,
        UpdateEmployeeSkillDto dto)
    {
        await _service.UpdateSkillAsync(employeeId, skillId, dto);

        return NoContent();
    }
}