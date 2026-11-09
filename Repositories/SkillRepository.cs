using EmployeeManagement.Data;
using EmployeeManagement.Models;
using EmployeeManagement.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Repositories;

public class SkillRepository: ISkillRepository
{
    public readonly AppDbContext _context;

    public SkillRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Skill>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Skills
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Skill?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Skills
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task AddAsync(Skill skill, CancellationToken cancellationToken)
    {
        await _context.Skills.AddAsync(skill, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}