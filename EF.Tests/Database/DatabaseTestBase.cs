using EmployeeManagement.Data;
using EmployeeManagement.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EF.Tests.Database;

/// <summary>
/// Base for database tests. Apply [Collection(DatabaseCollection.Name)] and
/// [Trait("Category", "Database")] to each concrete class.
/// </summary>
public abstract class DatabaseTestBase(PostgresFixture db) : IAsyncLifetime
{
    protected PostgresFixture Db { get; } = db;

    public Task InitializeAsync() => Db.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    protected AppDbContext NewContext() => Db.NewContext();

    protected async Task<Department> SeedDepartmentAsync(string name = "Engineering")
    {
        await using var ctx = NewContext();
        var department = new Department { Name = name };
        ctx.Departments.Add(department);
        await ctx.SaveChangesAsync();
        return department;
    }

    protected async Task<Employee> SeedEmployeeAsync(int departmentId, string email = "asha@test.com", string name = "Asha")
    {
        await using var ctx = NewContext();
        var employee = new Employee
        {
            Name = name, Email = email, Salary = 1000m,
            JoiningDate = new DateOnly(2024, 1, 15), DepartmentId = departmentId
        };
        ctx.Employees.Add(employee);
        await ctx.SaveChangesAsync();
        return employee;
    }

    protected async Task<Skill> SeedSkillAsync(string name = "C#")
    {
        await using var ctx = NewContext();
        var skill = new Skill { Name = name };
        ctx.Skills.Add(skill);
        await ctx.SaveChangesAsync();
        return skill;
    }

    protected async Task<Project> SeedProjectAsync(string name = "Apollo")
    {
        await using var ctx = NewContext();
        var project = new Project { Name = name, Description = "d" };
        ctx.Projects.Add(project);
        await ctx.SaveChangesAsync();
        return project;
    }

    /// <summary>Runs an action expected to fail in the database and returns PostgreSQL's own error.</summary>
    protected static async Task<PostgresException> ExpectPostgresErrorAsync(Func<Task> action)
    {
        var ex = await Assert.ThrowsAsync<DbUpdateException>(action);
        return Assert.IsType<PostgresException>(ex.InnerException);
    }

    /// <summary>
    /// Deleting a row that is still referenced (ON DELETE RESTRICT) is reported as 23001 by
    /// PostgreSQL 18 but as 23503 by PostgreSQL 17 and earlier. Both mean the same thing.
    /// </summary>
    protected static void AssertReferencedRowViolation(PostgresException pg, string constraint)
    {
        Assert.Contains(pg.SqlState, new[] { PostgresErrorCodes.RestrictViolation, PostgresErrorCodes.ForeignKeyViolation });
        Assert.Equal(constraint, pg.ConstraintName);
    }
}
