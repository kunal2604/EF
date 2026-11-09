using EmployeeManagement.Data.Projects;
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
    public async Task<IActionResult> GetAll()
    {
        var projects = await _projectService.GetAllAsync();
        
        return Ok(projects);
    }

    // GET: api/projects/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var project = await _projectService.GetByIdAsync(id);

        if(project is null)
            return NotFound();

        return Ok(project);
    }

    // POST: api/projects
    [HttpPost]
    public async Task<IActionResult> Create(CreateProjectDto dto)
    {
        var project = await _projectService.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = project.Id },
            project
        );
    }

    // POST: api/projects/1/employees
    [HttpPost("{projectId:int}/employees")]
    public async Task<IActionResult> AssignEmployee(int projectId, AssignEmployeeDto dto)
    {
        try
        {
            await _projectService.AssignEmployeeAsync(projectId, dto.EmployeeId);
            return NoContent();
        }
        catch(KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch(InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message});
        }
    }
}