namespace EmployeeManagement.DTOs.Employees;

public class EmployeeResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public DateOnly JoiningDate { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
}