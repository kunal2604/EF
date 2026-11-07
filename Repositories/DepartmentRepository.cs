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

    public async Task<List<Department>> GetAllAsync()
    {
        return await _context.Departments
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Department?> GetByIdAsync(int id)
    {
        return await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task AddAsync(Department department)
    {
        await _context.Departments.AddAsync(department);

        await _context.SaveChangesAsync();
    }
}