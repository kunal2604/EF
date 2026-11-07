namespace EmployeeManagement.Models;

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // Navigation Property
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}