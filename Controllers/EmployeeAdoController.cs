using Microsoft.AspNetCore.Mvc;
using EmployeeManagement.Services.Interfaces;

namespace EmployeeManagement.Controllers;

[ApiController]
[Route("api/ado/employees")]
public class EmployeeAdoController : ControllerBase
{
    private readonly IEmployeeAdoService _service;

    public EmployeeAdoController(IEmployeeAdoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllEmployees()
    {
        var employees = await _service.GetAllEmployeesAsync();

        return Ok(employees);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEmployeeById(int id)
    {
        var employee = await _service.GetEmployeeByIdAsync(id);

        if (employee == null)
            return NotFound();

        return Ok(employee);
    }
}