using EmployeeManagement.DTOs.Projects;

namespace EmployeeManagement.Services.Interfaces;

public interface IProjectService
{
    Task<List<ProjectResponseDto>> GetAllAsync();
    Task<ProjectResponseDto?> GetByIdAsync(int id);
    Task<ProjectResponseDto> CreateAsync(CreateProjectDto dto);
    Task AssignEmployeeAsync(int projectId, int employeeId);
}