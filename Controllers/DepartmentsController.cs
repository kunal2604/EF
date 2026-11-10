using EmployeeManagement.DTOs.Common;
using EmployeeManagement.DTOs.Departments;
using EmployeeManagement.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(
        IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    // GET: api/departments
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQuery query,
        CancellationToken cancellationToken)
    {
        var departments = await _departmentService.GetPagedAsync(query, cancellationToken);

        return Ok(departments);
    }

    // GET: api/departments/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var department = await _departmentService.GetByIdAsync(id, cancellationToken);

        if (department is null)
            return NotFound();

        return Ok(department);
    }

    // POST: api/departments
    [HttpPost]
    public async Task<IActionResult> Create(CreateDepartmentDto dto, CancellationToken cancellationToken)
    {
        var department = await _departmentService.CreateAsync(dto, cancellationToken);

        return Ok(department);
    }

    // PUT: api/departments/1
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateDepartmentDto dto, CancellationToken cancellationToken)
    {
        var updated = await _departmentService.UpdateAsync(id, dto, cancellationToken);

        if (!updated)
            return NotFound();

        return NoContent();
    }

    // DELETE: api/departments/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _departmentService.DeleteAsync(id, cancellationToken);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
