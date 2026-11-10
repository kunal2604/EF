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

    public async Task<(List<Skill> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Skills
            .AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        // Stable order, otherwise pages can overlap or skip rows.
        var items = await query
            .OrderBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Skill?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Skills
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        var lowered = name.ToLower();

        return await _context.Skills
            .AnyAsync(s => s.Name.ToLower() == lowered, cancellationToken);
    }

    public async Task AddAsync(Skill skill, CancellationToken cancellationToken)
    {
        await _context.Skills.AddAsync(skill, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}