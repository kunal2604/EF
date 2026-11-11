using EmployeeManagement.Models;
using EmployeeManagement.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EF.Tests.Database;

/// <summary>The repositories' real SQL: paging, ordering, includes, case-insensitive lookups.</summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public class RepositoryTests(PostgresFixture db) : DatabaseTestBase(db)
{
    private readonly CancellationToken _ct = CancellationToken.None;

    private async Task SeedEmployeesAsync(int departmentId, int count)
    {
        await using var ctx = NewContext();
        for (var i = 1; i <= count; i++)
            ctx.Employees.Add(new Employee
            {
                Name = $"Emp{i:00}", Email = $"emp{i:00}@test.com", Salary = i,
                JoiningDate = new DateOnly(2024, 1, 1), DepartmentId = departmentId
            });
        await ctx.SaveChangesAsync();
    }

    // ---- employees ----

    [Fact]
    public async Task Employees_are_paged_in_id_order_with_the_total()
    {
        var department = await SeedDepartmentAsync();
        await SeedEmployeesAsync(department.Id, 7);
        await using var ctx = NewContext();
        var sut = new EmployeeRepository(ctx);

        var (page2, total) = await sut.GetPagedAsync(page: 2, pageSize: 3, _ct);

        Assert.Equal(7, total);
        Assert.Equal(new[] { "Emp04", "Emp05", "Emp06" }, page2.Select(e => e.Name));
    }

    [Fact]
    public async Task The_last_page_is_partial_and_a_page_past_the_end_is_empty()
    {
        var department = await SeedDepartmentAsync();
        await SeedEmployeesAsync(department.Id, 7);
        await using var ctx = NewContext();
        var sut = new EmployeeRepository(ctx);

        var (last, _) = await sut.GetPagedAsync(3, 3, _ct);
        var (beyond, total) = await sut.GetPagedAsync(99, 3, _ct);

        Assert.Single(last);
        Assert.Empty(beyond);
        Assert.Equal(7, total);
    }

    [Fact]
    public async Task Pages_never_overlap_and_cover_every_row_exactly_once()
    {
        var department = await SeedDepartmentAsync();
        await SeedEmployeesAsync(department.Id, 11);
        await using var ctx = NewContext();
        var sut = new EmployeeRepository(ctx);

        var ids = new List<int>();
        for (var page = 1; page <= 4; page++)
            ids.AddRange((await sut.GetPagedAsync(page, 3, _ct)).Items.Select(e => e.Id));

        Assert.Equal(11, ids.Count);
        Assert.Equal(ids.Distinct().Count(), ids.Count);
        Assert.Equal(ids.OrderBy(i => i), ids);
    }

    [Fact]
    public async Task Paged_employees_come_with_their_department()
    {
        var department = await SeedDepartmentAsync("Platform");
        await SeedEmployeesAsync(department.Id, 2);
        await using var ctx = NewContext();

        var (items, _) = await new EmployeeRepository(ctx).GetPagedAsync(1, 10, _ct);

        Assert.All(items, e => Assert.Equal("Platform", e.DepartmentName));
    }

    [Fact]
    public async Task GetByEmail_ignores_case()
    {
        var department = await SeedDepartmentAsync();
        await SeedEmployeeAsync(department.Id, "asha@test.com");
        await using var ctx = NewContext();

        var found = await new EmployeeRepository(ctx).GetByEmailAsync("ASHA@Test.COM", _ct);

        Assert.NotNull(found);
    }

    [Fact]
    public async Task GetById_includes_the_department_and_projects()
    {
        var department = await SeedDepartmentAsync();
        var employee = await SeedEmployeeAsync(department.Id);
        await SeedProjectAsync();
        await using (var link = NewContext())
        {
            var e = await link.Employees.Include(x => x.Projects).SingleAsync();
            e.Projects.Add(await link.Projects.SingleAsync());
            await link.SaveChangesAsync();
        }
        await using var ctx = NewContext();

        var loaded = await new EmployeeRepository(ctx).GetByIdAsync(employee.Id, _ct);

        Assert.NotNull(loaded);
        Assert.Equal("Engineering", loaded.Department.Name);
        Assert.Single(loaded.Projects);
    }

    // ---- projects: paging combined with a collection include ----

    [Fact]
    public async Task Projects_page_correctly_while_loading_their_members()
    {
        var department = await SeedDepartmentAsync();
        await SeedEmployeesAsync(department.Id, 3);
        await using (var seed = NewContext())
        {
            var employees = await seed.Employees.OrderBy(e => e.Id).ToListAsync();
            var projects = Enumerable.Range(1, 3).Select(i => new Project { Name = $"P{i}", Description = "d" }).ToList();
            projects[0].Employees.Add(employees[0]);
            projects[0].Employees.Add(employees[1]);
            projects[1].Employees.Add(employees[2]);
            seed.Projects.AddRange(projects);
            await seed.SaveChangesAsync();
        }
        await using var ctx = NewContext();
        var sut = new ProjectRepository(ctx);

        var (first, total) = await sut.GetPagedAsync(1, 2, _ct);
        var (second, _) = await sut.GetPagedAsync(2, 2, _ct);

        Assert.Equal(3, total);
        Assert.Equal(new[] { "P1", "P2" }, first.Select(p => p.Name));
        Assert.Equal(2, first[0].EmployeeNames.Count);   // members were not lost to the row limit
        Assert.Single(first[1].EmployeeNames);
        Assert.Equal(new[] { "P3" }, second.Select(p => p.Name));
        Assert.Empty(second[0].EmployeeNames);
    }

    [Fact]
    public async Task Employee_existence_check_is_true_only_for_real_ids_and_loads_nothing()
    {
        var department = await SeedDepartmentAsync();
        var employee = await SeedEmployeeAsync(department.Id);
        await using var ctx = NewContext();
        var sut = new EmployeeRepository(ctx);

        Assert.True(await sut.ExistsAsync(employee.Id, _ct));
        Assert.False(await sut.ExistsAsync(employee.Id + 1, _ct));
        Assert.Empty(ctx.ChangeTracker.Entries());
    }

    // ---- reads are projected straight into DTOs ----

    [Fact]
    public async Task Employee_reads_are_projected_and_nothing_is_tracked()
    {
        var department = await SeedDepartmentAsync("Platform");
        var employee = await SeedEmployeeAsync(department.Id);
        await using var ctx = NewContext();
        var sut = new EmployeeRepository(ctx);

        var dto = await sut.GetResponseByIdAsync(employee.Id, _ct);
        var (page, _) = await sut.GetPagedAsync(1, 10, _ct);

        Assert.NotNull(dto);
        Assert.Equal(("Asha", "asha@test.com", "Platform"), (dto.Name, dto.Email, dto.DepartmentName));
        Assert.Equal(employee.Id, Assert.Single(page).Id);
        Assert.Empty(ctx.ChangeTracker.Entries()); // no entities were materialised
    }

    [Fact]
    public async Task Employee_response_by_id_is_null_when_missing()
    {
        await SeedDepartmentAsync();
        await using var ctx = NewContext();

        Assert.Null(await new EmployeeRepository(ctx).GetResponseByIdAsync(999, _ct));
    }

    [Fact]
    public async Task Project_reads_are_projected_with_member_names_and_nothing_is_tracked()
    {
        var department = await SeedDepartmentAsync();
        await SeedEmployeesAsync(department.Id, 2);
        await SeedProjectAsync("Apollo");
        await using (var link = NewContext())
        {
            var project = await link.Projects.Include(p => p.Employees).SingleAsync();
            foreach (var e in await link.Employees.ToListAsync()) project.Employees.Add(e);
            await link.SaveChangesAsync();
        }
        await using var ctx = NewContext();
        var sut = new ProjectRepository(ctx);

        var dto = await sut.GetResponseByIdAsync(1, _ct);
        var (page, _) = await sut.GetPagedAsync(1, 10, _ct);

        Assert.NotNull(dto);
        Assert.Equal("Apollo", dto.Name);
        Assert.Equal(new[] { "Emp01", "Emp02" }, dto.EmployeeNames.Order());
        Assert.Equal(2, Assert.Single(page).EmployeeNames.Count);
        Assert.Empty(ctx.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Project_response_by_id_is_null_when_missing_and_has_no_members_when_unassigned()
    {
        await SeedProjectAsync();
        await using var ctx = NewContext();
        var sut = new ProjectRepository(ctx);

        Assert.Null(await sut.GetResponseByIdAsync(999, _ct));
        Assert.Empty((await sut.GetResponseByIdAsync(1, _ct))!.EmployeeNames);
    }

    [Fact]
    public async Task Employee_skills_are_projected_with_the_skill_name_and_only_for_that_employee()
    {
        var department = await SeedDepartmentAsync();
        var asha = await SeedEmployeeAsync(department.Id, "asha@test.com");
        var ben = await SeedEmployeeAsync(department.Id, "ben@test.com", "Ben");
        var skill = await SeedSkillAsync("SQL");
        await using (var seed = NewContext())
        {
            seed.EmployeeSkills.AddRange(
                new EmployeeSkill { EmployeeId = asha.Id, SkillId = skill.Id, ProficiencyLevel = "Expert", YearsOfExperience = 9 },
                new EmployeeSkill { EmployeeId = ben.Id, SkillId = skill.Id, ProficiencyLevel = "Basic", YearsOfExperience = 1 });
            await seed.SaveChangesAsync();
        }
        await using var ctx = NewContext();

        var result = await new EmployeeSkillRepository(ctx).GetByEmployeeIdAsync(asha.Id, _ct);

        var item = Assert.Single(result);
        Assert.Equal(("SQL", "Expert", 9), (item.SkillName, item.ProficiencyLevel, item.YearsOfExperience));
        Assert.Empty(ctx.ChangeTracker.Entries());
    }

    // ---- departments: name uniqueness check ----

    [Theory]
    [InlineData("Engineering", null, true)]
    [InlineData("engineering", null, true)]   // case-insensitive
    [InlineData("Engineering ", null, false)] // no implicit trimming
    [InlineData("Legal", null, false)]
    public async Task Department_name_check_ignores_case(string name, int? excludeId, bool expected)
    {
        await SeedDepartmentAsync("Engineering");
        await using var ctx = NewContext();

        Assert.Equal(expected, await new DepartmentRepository(ctx).NameExistsAsync(name, excludeId, _ct));
    }

    [Fact]
    public async Task Department_name_check_can_exclude_the_department_itself()
    {
        var department = await SeedDepartmentAsync("Engineering");
        await using var ctx = NewContext();
        var sut = new DepartmentRepository(ctx);

        Assert.False(await sut.NameExistsAsync("ENGINEERING", department.Id, _ct)); // renaming to its own name is fine
        Assert.True(await sut.NameExistsAsync("ENGINEERING", department.Id + 1, _ct));
    }

    // ---- skills ----

    [Theory]
    [InlineData("C#", true)]
    [InlineData("c#", true)]    // case-insensitive, though the unique index itself is case-sensitive
    [InlineData("C# ", false)]  // no implicit trimming
    [InlineData("Go", false)]
    public async Task Skill_existence_check_ignores_case(string name, bool expected)
    {
        await SeedSkillAsync("C#");
        await using var ctx = NewContext();

        Assert.Equal(expected, await new SkillRepository(ctx).ExistsByNameAsync(name, _ct));
    }

    [Fact]
    public async Task Skills_page_in_id_order()
    {
        foreach (var name in new[] { "A", "B", "C", "D", "E" })
            await SeedSkillAsync(name);
        await using var ctx = NewContext();

        var (items, total) = await new SkillRepository(ctx).GetPagedAsync(2, 2, _ct);

        Assert.Equal(5, total);
        Assert.Equal(new[] { "C", "D" }, items.Select(s => s.Name));
    }

    // ---- departments ----

    [Fact]
    public async Task Departments_page_with_a_total()
    {
        foreach (var name in new[] { "A", "B", "C" })
            await SeedDepartmentAsync(name);
        await using var ctx = NewContext();

        var (items, total) = await new DepartmentRepository(ctx).GetPagedAsync(1, 2, _ct);

        Assert.Equal(3, total);
        Assert.Equal(2, items.Count);
    }

    // ---- cancellation ----

    [Fact]
    public async Task A_cancelled_token_stops_the_query()
    {
        await SeedDepartmentAsync();
        await using var ctx = NewContext();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new EmployeeRepository(ctx).GetPagedAsync(1, 10, cts.Token));
    }
}
