using EmployeeManagement.DTOs.Common;
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

    public async Task<PagedResult<ProjectResponseDto>> GetPagedAsync(
        PagedQuery query, CancellationToken cancellationToken)
    {
        var (projects, totalCount) = await _projectRepository.GetPagedAsync(
            query.Page, query.PageSize, cancellationToken);

        var items = projects.Select(p => new ProjectResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            EmployeeNames = p.Employees.Select(e => e.Name).ToList()
        }).ToList();

        return PagedResult<ProjectResponseDto>.Create(items, totalCount, query);
    }

    public async Task<ProjectResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(id, cancellationToken);

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

    public async Task<ProjectResponseDto> CreateAsync(CreateProjectDto dto, CancellationToken cancellationToken)
    {
        var project = new Project
        {
            Name = dto.Name,
            Description = dto.Description
        };

        await _projectRepository.AddAsync(project, cancellationToken);

        return new ProjectResponseDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description 
        };
    }

    public async Task AssignEmployeeAsync(int projectId, int employeeId, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);

        if(project is null)
            throw new NotFoundException("Project not found !");

        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);

        if(employee is null)
            throw new NotFoundException("Employee not found !");
        
        if(employee.Projects.Any(p => p.Id == projectId))
            throw new ConflictException("Employee is already assigned to this project !");
        
        employee.Projects.Add(project);

        await _employeeRepository.UpdateAsync(employee, cancellationToken);
    }

    public async Task RemoveEmployeeAsync(int projectId, int employeeId, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);

        if(project is null)
            throw new NotFoundException("Project not found !");

        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);

        if(employee is null)
            throw new NotFoundException("Employee not found !");

        var assignedProject = employee.Projects.FirstOrDefault(p => p.Id == projectId);

        if(assignedProject is null)
            throw new NotFoundException("Employee is not assigned to this project !");

        employee.Projects.Remove(assignedProject);

        await _employeeRepository.UpdateAsync(employee, cancellationToken);
    }
}
