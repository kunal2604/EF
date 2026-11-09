namespace EmployeeManagement.DTOs.EmployeeSkills;

public class EmployeeSkillResponseDto
{
    public int EmployeeId { get; set; }
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string ProficiencyLevel { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
}