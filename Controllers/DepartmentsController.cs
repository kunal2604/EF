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
    public async Task<IActionResult> GetAll()
    {
        var departments = await _departmentService.GetAllAsync();

        return Ok(departments);
    }

    // POST: api/departments
    [HttpPost]
    public async Task<IActionResult> Create(CreateDepartmentDto dto)
    {
        var department = await _departmentService.CreateAsync(dto);

        return Ok(department);
    }
}