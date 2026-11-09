using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.DTOs.EmployeeSkills;

public class AssignSkillDto
{
    [Range(1, int.MaxValue)]
    public int SkillId { get; set; }
    [Required]
    [MaxLength(30)]
    public string ProficiencyLevel { get; set; } = string.Empty;
    [Range(0,50)]
    public int YearsOfExperience { get; set; }
}