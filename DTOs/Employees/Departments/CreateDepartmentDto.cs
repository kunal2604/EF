using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.DTOs.Departments;

public class CreateDepartmentDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}