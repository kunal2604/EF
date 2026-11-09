using EmployeeManagement.Data;
using EmployeeManagement.Models;
using EmployeeManagement.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Repositories;

public class EmployeeSkillRepository : IEmployeeSkillRepository
{
    private readonly AppDbContext _context;

    public EmployeeSkillRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<EmployeeSkill>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _context.EmployeeSkills
            .AsNoTracking()
            .Include(es => es.Skill)
            .Where(es => es.EmployeeId == employeeId)
            .ToListAsync();
    }

    public async Task<EmployeeSkill?> GetAsync(int employeeId, int skillId)
    {
        return await _context.EmployeeSkills
            .FirstOrDefaultAsync(es =>
                es.EmployeeId == employeeId && es.SkillId == skillId);
    }

    public async Task AddAsync(EmployeeSkill employeeSkill)
    {
        await _context.EmployeeSkills.AddAsync(employeeSkill);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(EmployeeSkill employeeSkill)
    {
        await _context.SaveChangesAsync();
    }
}