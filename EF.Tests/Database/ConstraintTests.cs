using EmployeeManagement.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EF.Tests.Database;

/// <summary>What the database itself enforces, with PostgreSQL's real error codes.</summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public class ConstraintTests(PostgresFixture db) : DatabaseTestBase(db)
{
    private static Employee NewEmployee(int departmentId, string email = "asha@test.com", string name = "Asha", decimal salary = 1000m) =>
        new() { Name = name, Email = email, Salary = salary, JoiningDate = new DateOnly(2024, 1, 15), DepartmentId = departmentId };

    // ---- uniqueness ----

    [Theory]
    [InlineData("asha@test.com", "asha@test.com")]
    [InlineData("asha@test.com", "ASHA@TEST.COM")]   // citext: differing only in case is a duplicate
    [InlineData("Asha@Test.Com", "aSHA@tEST.cOM")]
    public async Task Employee_email_must_be_unique_ignoring_case(string first, string second)
    {
        var department = await SeedDepartmentAsync();
        await SeedEmployeeAsync(department.Id, first);

        await using var ctx = NewContext();
        ctx.Employees.Add(NewEmployee(department.Id, second));
        var pg = await ExpectPostgresErrorAsync(() => ctx.SaveChangesAsync());

        Assert.Equal(PostgresErrorCodes.UniqueViolation, pg.SqlState);
        Assert.Equal("IX_Employees_Email", pg.ConstraintName);
    }

    [Fact]
    public async Task Skill_name_must_be_unique()
    {
        await SeedSkillAsync("C#");

        await using var ctx = NewContext();
        ctx.Skills.Add(new Skill { Name = "C#" });
        var pg = await ExpectPostgresErrorAsync(() => ctx.SaveChangesAsync());

        Assert.Equal(PostgresErrorCodes.UniqueViolation, pg.SqlState);
        Assert.Equal("IX_Skills_Name", pg.ConstraintName);
    }

    [Fact]
    public async Task A_skill_cannot_be_assigned_to_the_same_employee_twice()
    {
        var department = await SeedDepartmentAsync();
        var employee = await SeedEmployeeAsync(department.Id);
        var skill = await SeedSkillAsync();
        await using (var first = NewContext())
        {
            first.EmployeeSkills.Add(new EmployeeSkill { EmployeeId = employee.Id, SkillId = skill.Id, ProficiencyLevel = "Basic", YearsOfExperience = 1 });
            await first.SaveChangesAsync();
        }

        await using var ctx = NewContext();
        ctx.EmployeeSkills.Add(new EmployeeSkill { EmployeeId = employee.Id, SkillId = skill.Id, ProficiencyLevel = "Expert", YearsOfExperience = 9 });
        var pg = await ExpectPostgresErrorAsync(() => ctx.SaveChangesAsync());

        Assert.Equal(PostgresErrorCodes.UniqueViolation, pg.SqlState);
        Assert.Equal("PK_EmployeeSkills", pg.ConstraintName);
    }

    // ---- column rules ----

    [Fact]
    public async Task Years_of_experience_cannot_be_negative()
    {
        var department = await SeedDepartmentAsync();
        var employee = await SeedEmployeeAsync(department.Id);
        var skill = await SeedSkillAsync();

        await using var ctx = NewContext();
        ctx.EmployeeSkills.Add(new EmployeeSkill { EmployeeId = employee.Id, SkillId = skill.Id, ProficiencyLevel = "Basic", YearsOfExperience = -1 });
        var pg = await ExpectPostgresErrorAsync(() => ctx.SaveChangesAsync());

        Assert.Equal(PostgresErrorCodes.CheckViolation, pg.SqlState);
        Assert.Equal("CK_EmployeeSkills_YearsOfExperience", pg.ConstraintName);
    }

    [Fact]
    public async Task Employee_name_is_limited_to_100_characters()
    {
        var department = await SeedDepartmentAsync();

        await using var ctx = NewContext();
        ctx.Employees.Add(NewEmployee(department.Id, name: new string('x', 101)));
        var pg = await ExpectPostgresErrorAsync(() => ctx.SaveChangesAsync());

        Assert.Equal(PostgresErrorCodes.StringDataRightTruncation, pg.SqlState);
    }

    [Fact]
    public async Task Salary_stores_the_largest_value_the_API_accepts()
    {
        // The DTO allows up to 9999999999999999.99; the column must hold exactly that.
        var department = await SeedDepartmentAsync();
        await using (var write = NewContext())
        {
            write.Employees.Add(NewEmployee(department.Id, salary: 9999999999999999.99m));
            await write.SaveChangesAsync();
        }

        await using var read = NewContext();
        Assert.Equal(9999999999999999.99m, (await read.Employees.SingleAsync()).Salary);
    }

    [Fact]
    public async Task Salary_one_digit_above_the_limit_is_rejected_by_the_database()
    {
        var department = await SeedDepartmentAsync();

        await using var ctx = NewContext();
        ctx.Employees.Add(NewEmployee(department.Id, salary: 10000000000000000m));
        var pg = await ExpectPostgresErrorAsync(() => ctx.SaveChangesAsync());

        Assert.Equal(PostgresErrorCodes.NumericValueOutOfRange, pg.SqlState);
    }

    [Fact]
    public async Task Salary_keeps_two_decimal_places()
    {
        var department = await SeedDepartmentAsync();
        await using (var write = NewContext())
        {
            write.Employees.Add(NewEmployee(department.Id, salary: 123.456m));
            await write.SaveChangesAsync();
        }

        await using var read = NewContext();
        Assert.Equal(123.46m, (await read.Employees.SingleAsync()).Salary); // rounded by numeric(18,2)
    }

    [Fact]
    public async Task An_unset_joining_date_gets_the_database_default_of_today()
    {
        var department = await SeedDepartmentAsync();
        await using (var write = NewContext())
        {
            var employee = NewEmployee(department.Id);
            employee.JoiningDate = default; // 0001-01-01 is EF's "not set"
            write.Employees.Add(employee);
            await write.SaveChangesAsync();
        }

        await using var read = NewContext();
        var stored = (await read.Employees.SingleAsync()).JoiningDate;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.NotEqual(default, stored);
        Assert.InRange(stored, today.AddDays(-1), today.AddDays(1)); // tolerant of timezone at midnight
    }

    [Fact]
    public async Task Email_lookup_is_case_insensitive()
    {
        var department = await SeedDepartmentAsync();
        await SeedEmployeeAsync(department.Id, "asha@test.com");

        await using var ctx = NewContext();
        var found = await ctx.Employees.FirstOrDefaultAsync(e => e.Email == "ASHA@TEST.COM");

        Assert.NotNull(found);
    }

    // ---- RESTRICT: nothing referenced may be deleted ----

    [Fact]
    public async Task A_department_with_employees_cannot_be_deleted()
    {
        var department = await SeedDepartmentAsync();
        await SeedEmployeeAsync(department.Id);

        await using var ctx = NewContext();
        ctx.Departments.Remove(await ctx.Departments.SingleAsync());
        var pg = await ExpectPostgresErrorAsync(() => ctx.SaveChangesAsync());

        AssertReferencedRowViolation(pg, "FK_Employees_Departments_DepartmentId");
    }

    [Fact]
    public async Task An_employee_with_skills_cannot_be_deleted()
    {
        var department = await SeedDepartmentAsync();
        var employee = await SeedEmployeeAsync(department.Id);
        var skill = await SeedSkillAsync();
        await using (var assign = NewContext())
        {
            assign.EmployeeSkills.Add(new EmployeeSkill { EmployeeId = employee.Id, SkillId = skill.Id, ProficiencyLevel = "Basic", YearsOfExperience = 1 });
            await assign.SaveChangesAsync();
        }

        await using var ctx = NewContext();
        ctx.Employees.Remove(await ctx.Employees.SingleAsync());
        var pg = await ExpectPostgresErrorAsync(() => ctx.SaveChangesAsync());

        AssertReferencedRowViolation(pg, "FK_EmployeeSkills_Employees_EmployeeId");
    }

    [Fact]
    public async Task A_skill_that_is_assigned_cannot_be_deleted()
    {
        var department = await SeedDepartmentAsync();
        var employee = await SeedEmployeeAsync(department.Id);
        var skill = await SeedSkillAsync();
        await using (var assign = NewContext())
        {
            assign.EmployeeSkills.Add(new EmployeeSkill { EmployeeId = employee.Id, SkillId = skill.Id, ProficiencyLevel = "Basic", YearsOfExperience = 1 });
            await assign.SaveChangesAsync();
        }

        await using var ctx = NewContext();
        ctx.Skills.Remove(await ctx.Skills.SingleAsync());
        var pg = await ExpectPostgresErrorAsync(() => ctx.SaveChangesAsync());

        AssertReferencedRowViolation(pg, "FK_EmployeeSkills_Skills_SkillId");
    }

    [Fact]
    public async Task A_project_with_members_is_protected_at_the_database_level()
    {
        var department = await SeedDepartmentAsync();
        var employee = await SeedEmployeeAsync(department.Id);
        var project = await SeedProjectAsync();
        await using (var assign = NewContext())
        {
            var loaded = await assign.Employees.Include(e => e.Projects).SingleAsync();
            loaded.Projects.Add(await assign.Projects.SingleAsync());
            await assign.SaveChangesAsync();
        }

        // Raw SQL bypasses EF's own tracking check, so this proves the database rule itself.
        await using var ctx = NewContext();
        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            ctx.Database.ExecuteSqlAsync($"""DELETE FROM "Projects" WHERE "Id" = {project.Id}"""));

        AssertReferencedRowViolation(ex, "FK_EmployeeProjects_Projects_ProjectId");
    }

    [Fact]
    public async Task An_employee_cannot_reference_a_department_that_does_not_exist()
    {
        await using var ctx = NewContext();
        ctx.Employees.Add(NewEmployee(departmentId: 424242));
        var pg = await ExpectPostgresErrorAsync(() => ctx.SaveChangesAsync());

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, pg.SqlState);
    }
}
