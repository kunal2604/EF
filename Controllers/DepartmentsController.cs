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
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var departments = await _departmentService.GetAllAsync(cancellationToken);

        return Ok(departments);
    }

    // POST: api/departments
    [HttpPost]
    public async Task<IActionResult> Create(CreateDepartmentDto dto, CancellationToken cancellationToken)
    {
        var department = await _departmentService.CreateAsync(dto, cancellationToken);

        return Ok(department);
    }
}