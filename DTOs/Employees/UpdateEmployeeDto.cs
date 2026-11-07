namespace EmployeeManagement.DTOs.Employees;

public class UpdateEmployeeDto
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public decimal Salary { get; set; }

    public int DepartmentId { get; set; }
}