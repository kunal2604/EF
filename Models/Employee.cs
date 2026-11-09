namespace EmployeeManagement.Models;

public class Employee
{
    public int Id {get; set; }
    public string Name {get; set; } = string.Empty;
    public string Email {get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public DateOnly JoiningDate { get; set; }
    // Foreign Key
    public int DepartmentId { get; set; }
    // Navigation Property
    public Department Department { get; set; } = null!;
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<EmployeeSkill> EmployeeSkills { get; set; } = new List<EmployeeSkill>();
}