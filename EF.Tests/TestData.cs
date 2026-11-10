using EmployeeManagement.Models;

namespace EF.Tests;

/// <summary>Small builders so each test only spells out what it cares about.</summary>
internal static class TestData
{
    public static Department Department(int id = 1, string name = "Engineering") =>
        new() { Id = id, Name = name };

    public static Employee Employee(int id = 1, string email = "asha@test.com", Department? department = null)
    {
        department ??= Department();
        return new Employee
        {
            Id = id,
            Name = "Asha",
            Email = email,
            Salary = 50000m,
            JoiningDate = new DateOnly(2024, 1, 15),
            DepartmentId = department.Id,
            Department = department
        };
    }

    public static Project Project(int id = 1, string name = "Apollo") =>
        new() { Id = id, Name = name, Description = "desc" };

    public static Skill Skill(int id = 1, string name = "C#") =>
        new() { Id = id, Name = name };

    public static EmployeeSkill EmployeeSkill(int employeeId = 1, Skill? skill = null)
    {
        skill ??= Skill();
        return new EmployeeSkill
        {
            EmployeeId = employeeId,
            SkillId = skill.Id,
            Skill = skill,
            ProficiencyLevel = "Advanced",
            YearsOfExperience = 3
        };
    }
}
