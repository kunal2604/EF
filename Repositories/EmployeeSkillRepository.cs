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

    public async Task<List<EmployeeSkill>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken)
    {
        return await _context.EmployeeSkills
            .AsNoTracking()
            .Include(es => es.Skill)
            .Where(es => es.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmployeeSkill?> GetAsync(int employeeId, int skillId, CancellationToken cancellationToken)
    {
        return await _context.EmployeeSkills
            .FirstOrDefaultAsync(es =>
                es.EmployeeId == employeeId && es.SkillId == skillId, cancellationToken);
    }

    public async Task AddAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken)
    {
        await _context.EmployeeSkills.AddAsync(employeeSkill, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken)
    {
        _context.EmployeeSkills.Remove(employeeSkill);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
