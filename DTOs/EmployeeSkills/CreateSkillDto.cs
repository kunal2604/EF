using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.DTOs.EmployeeSkills;

public class CreateSkillDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}