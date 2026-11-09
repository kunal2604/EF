using EmployeeManagement.Exceptions;
using EmployeeManagement.DTOs.Projects;
using EmployeeManagement.Models;
using EmployeeManagement.Repositories.Interfaces;
using EmployeeManagement.Services.Interfaces;

namespace EmployeeManagement.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public ProjectService(
        IProjectRepository projectRepository, 
        IEmployeeRepository employeeRepository)
    {
        _projectRepository = projectRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<List<ProjectResponseDto>> GetAllAsync()
    {
        var projects = await _projectRepository.GetAllAsync();

        return projects.Select(p => new ProjectResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            EmployeeNames = p.Employees.Select(e => e.Name).ToList()
        }).ToList();
    }

    public async Task<ProjectResponseDto?> GetByIdAsync(int id)
    {
        var project = await _projectRepository.GetByIdAsync(id);

        if(project is null)
            return null;

        return new ProjectResponseDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            EmployeeNames = project.Employees.Select(e => e.Name).ToList()  
        };
    }

    public async Task<ProjectResponseDto> CreateAsync(CreateProjectDto dto)
    {
        var project = new Project
        {
            Name = dto.Name,
            Description = dto.Description
        };

        await _projectRepository.AddAsync(project);

        return new ProjectResponseDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description 
        };
    }

    public async Task AssignEmployeeAsync(int projectId, int employeeId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);

        if(project is null)
            throw new NotFoundException("Project not found !");

        var employee = await _employeeRepository.GetByIdAsync(employeeId);

        if(employee is null)
            throw new NotFoundException("Employee not found !");
        
        if(employee.Projects.Any(p => p.Id == projectId))
            throw new ConflictException("Employee is already assigned to this project !");
        
        employee.Projects.Add(project);

        await _employeeRepository.UpdateAsync(employee);
    }
}