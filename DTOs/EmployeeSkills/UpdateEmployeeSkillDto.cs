using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.DTOs.EmployeeSkills;

public class UpdateEmployeeSkillDto
{
    [Required]
    [MaxLength(100)]
    public string ProficiencyLevel { get; set; } = string.Empty;
    [Range(0,50)]
    public int YearsOfExperience { get; set; }
}