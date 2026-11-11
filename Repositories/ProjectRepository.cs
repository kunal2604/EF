using EmployeeManagement.Data;
using EmployeeManagement.DTOs.Projects;
using EmployeeManagement.Mappings;
using EmployeeManagement.Models;
using EmployeeManagement.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<ProjectResponseDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await _context.Projects.CountAsync(cancellationToken);

        // Stable order, otherwise pages can overlap or skip rows. Only the project columns and
        // the employees' names are loaded, not whole Employee rows.
        var items = await _context.Projects
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ResponseMappings.ProjectProjection)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<ProjectResponseDto?> GetResponseByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Projects
            .Where(p => p.Id == id)
            .Select(ResponseMappings.ProjectProjection)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Projects
            .Include(p => p.Employees)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken)
    {
        await _context.Projects.AddAsync(project, cancellationToken);
        
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Project project, CancellationToken cancellationToken)
    {
        _context.Projects.Remove(project);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
