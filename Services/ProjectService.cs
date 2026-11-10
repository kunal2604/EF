using EmployeeManagement.Mappings;
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

        var items = projects.Select(p => p.ToResponseDto()).ToList();

        return PagedResult<ProjectResponseDto>.Create(items, totalCount, query);
    }

    public async Task<ProjectResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(id, cancellationToken);

        if(project is null)
            return null;

        return project.ToResponseDto();
    }

    public async Task<ProjectResponseDto> CreateAsync(CreateProjectDto dto, CancellationToken cancellationToken)
    {
        var project = new Project
        {
            Name = dto.Name,
            Description = dto.Description
        };

        await _projectRepository.AddAsync(project, cancellationToken);

        return project.ToResponseDto();
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

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(id, cancellationToken);

        if(project is null)
            return false;

        // GetByIdAsync already loaded Employees; EF would otherwise throw while
        // tracking the join rows, before the database ever sees the delete.
        if(project.Employees.Count > 0)
            throw new ConflictException(
                "This project can't be deleted while employees are assigned to it. Remove them from the project first.");

        await _projectRepository.DeleteAsync(project, cancellationToken);

        return true;
    }
}
