using EmployeeManagement.DTOs.Common;
using EmployeeManagement.DTOs.Projects;
using EmployeeManagement.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    // GET: api/projects
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQuery query,
        CancellationToken cancellationToken)
    {
        var projects = await _projectService.GetPagedAsync(query, cancellationToken);
        
        return Ok(projects);
    }

    // GET: api/projects/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var project = await _projectService.GetByIdAsync(id, cancellationToken);

        if(project is null)
            return NotFound();

        return Ok(project);
    }

    // POST: api/projects
    [HttpPost]
    public async Task<IActionResult> Create(CreateProjectDto dto, CancellationToken cancellationToken)
    {
        var project = await _projectService.CreateAsync(dto, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = project.Id },
            project
        );
    }

    // POST: api/projects/1/employees
    [HttpPost("{projectId:int}/employees")]
    public async Task<IActionResult> AssignEmployee(int projectId, AssignEmployeeDto dto, CancellationToken cancellationToken)
    {
        await _projectService.AssignEmployeeAsync(projectId, dto.EmployeeId, cancellationToken);
        return NoContent();
    }

    // DELETE: api/projects/1/employees/2
    [HttpDelete("{projectId:int}/employees/{employeeId:int}")]
    public async Task<IActionResult> RemoveEmployee(int projectId, int employeeId, CancellationToken cancellationToken)
    {
        await _projectService.RemoveEmployeeAsync(projectId, employeeId, cancellationToken);
        return NoContent();
    }

    // DELETE: api/projects/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _projectService.DeleteAsync(id, cancellationToken);

        if(!deleted)
            return NotFound();

        return NoContent();
    }
}
