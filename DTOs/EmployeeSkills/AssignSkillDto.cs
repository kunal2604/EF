using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.DTOs.EmployeeSkills;

public class AssignSkillDto
{
    public int SkillId { get; set; }
    [Required]
    [MaxLength(30)]
    public string ProficiencyLevel { get; set; } = string.Empty;
    [Range(0,50)]
    public int YearsOfExperience { get; set; }
}