using EmployeeManagement.DTOs.Employees;
using EmployeeManagement.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    // GET: api/employees
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var employees = await _employeeService.GetAllAsync();

        return Ok(employees);
    }

    // GET: api/employees/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var employee = await _employeeService.GetByIdAsync(id);

        if(employee is null)
            return NotFound();
        
        return Ok(employee);
    }

    // POST: api/employees
    [HttpPost]
    public async Task<IActionResult> Create(CreateEmployeeDto dto)
    {
        var employee = await _employeeService.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = employee.Id },
            employee
        );
    }

    // PUT: api/employees/1
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateEmployeeDto dto)
    {
        var updated = await _employeeService.UpdateAsync(id, dto);

        if(!updated)
            return NotFound();
            
        return NoContent();
    }

    // DELETE: api/employees/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _employeeService.DeleteAsync(id);

        if(!deleted)
            return NotFound();
        
        return NoContent();
    }
}