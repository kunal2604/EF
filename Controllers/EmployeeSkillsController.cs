using EmployeeManagement.DTOs.EmployeeSkills;
using EmployeeManagement.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Controllers;

[ApiController]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
[Route("api/employees/{employeeId:int}/skills")]
public class EmployeeSkillsController : ControllerBase
{
    private readonly IEmployeeSkillService _service;

    public EmployeeSkillsController(IEmployeeSkillService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> AssignSkill(
        int employeeId,
        AssignSkillDto dto,
        CancellationToken cancellationToken)
    {
        await _service.AssignSkillAsync(employeeId, dto, cancellationToken);

        return NoContent();
    }

    [HttpGet]
    [ProducesResponseType<List<EmployeeSkillResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetEmployeeSkills(int employeeId, CancellationToken cancellationToken)
    {
        var skills = await _service.GetEmployeeSkillsAsync(employeeId, cancellationToken);

        return Ok(skills);
    }

    [HttpPut("{skillId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UpdateSkill(
        int employeeId,
        int skillId,
        UpdateEmployeeSkillDto dto,
        CancellationToken cancellationToken)
    {
        await _service.UpdateSkillAsync(employeeId, skillId, dto, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{skillId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> RemoveSkill(
        int employeeId,
        int skillId,
        CancellationToken cancellationToken)
    {
        await _service.RemoveSkillAsync(employeeId, skillId, cancellationToken);

        return NoContent();
    }
}
