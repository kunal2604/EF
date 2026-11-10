using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.DTOs.Departments;

public class UpdateDepartmentDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
