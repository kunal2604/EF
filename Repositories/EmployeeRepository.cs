using EmployeeManagement.Data;
using EmployeeManagement.DTOs.Employees;
using EmployeeManagement.Mappings;
using EmployeeManagement.Models;
using EmployeeManagement.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;

    public EmployeeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<EmployeeResponseDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await _context.Employees.CountAsync(cancellationToken);

        // Stable order, otherwise pages can overlap or skip rows. Select() makes EF load
        // only the columns the DTO needs (and nothing is tracked).
        var items = await _context.Employees
            .OrderBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ResponseMappings.EmployeeProjection)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<EmployeeResponseDto?> GetResponseByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Employees
            .Where(e => e.Id == id)
            .Select(ResponseMappings.EmployeeProjection)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Projects)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await _context.Employees
            .FirstOrDefaultAsync(e => e.Email == email, cancellationToken);
    }

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken)
    {
        await _context.Employees.AddAsync(employee, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Employee employee, CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Employee employee, CancellationToken cancellationToken)
    {
        _context.Employees.Remove(employee);

        await _context.SaveChangesAsync(cancellationToken);
    }
}