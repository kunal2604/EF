using Microsoft.AspNetCore.Mvc;
using EmployeeManagement.Models;
using EmployeeManagement.Services.Interfaces;

namespace EmployeeManagement.Controllers;

[ApiController]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
[Route("api/ado/employees")]
public class EmployeeAdoController : ControllerBase
{
    private readonly IEmployeeAdoService _service;

    public EmployeeAdoController(IEmployeeAdoService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType<List<Employee>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllEmployees()
    {
        var employees = await _service.GetAllEmployeesAsync();

        return Ok(employees);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<Employee>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployeeById(int id)
    {
        var employee = await _service.GetEmployeeByIdAsync(id);

        if (employee == null)
            return NotFound();

        return Ok(employee);
    }
}