using EmployeeManagement.Data;
using EmployeeManagement.Models;
using EmployeeManagement.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly AppDbContext _context;

    public DepartmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Department>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Departments
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Department?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task AddAsync(Department department, CancellationToken cancellationToken)
    {
        await _context.Departments.AddAsync(department, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }
}