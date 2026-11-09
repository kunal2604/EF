using EmployeeManagement.Data.Projects;
using EmployeeManagement.DTOs.Projects;
using EmployeeManagement.Models;

namespace EmployeeManagement.Services.Interfaces;

public interface IProjectService
{
    Task<List<ProjectResponseDto>> GetAllAsync();
    Task<ProjectResponseDto?> GetByIdAsync(int id);
    Task<ProjectResponseDto> CreateAsync(CreateProjectDto dto);
    Task AssignEmployeeAsync(int projectId, int employeeId);
}