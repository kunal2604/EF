using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.DTOs.Employees;

public class CreateEmployeeDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Range(0, 999999999999999.99)]
    public decimal Salary { get; set; }

    public DateOnly JoiningDate { get; set; }

    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }
}
