using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.DTOs.Projects;

public class CreateProjectDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
}
