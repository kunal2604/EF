using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.DTOs.Projects;

public class AssignEmployeeDto
{
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }
}
