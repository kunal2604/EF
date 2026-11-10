using EmployeeManagement.DTOs.Departments;
using EmployeeManagement.DTOs.EmployeeSkills;
using EmployeeManagement.DTOs.Employees;
using EmployeeManagement.DTOs.Projects;
using EmployeeManagement.Models;

namespace EmployeeManagement.Mappings;

/// <summary>
/// Entity → response DTO mappings, defined once so a new field only
/// has to be added in one place.
/// </summary>
public static class ResponseMappings
{
    public static DepartmentResponseDto ToResponseDto(this Department department) => new()
    {
        Id = department.Id,
        Name = department.Name
    };

    /// <remarks>Requires <see cref="Employee.Department"/> to be loaded.</remarks>
    public static EmployeeResponseDto ToResponseDto(this Employee employee) => new()
    {
        Id = employee.Id,
        Name = employee.Name,
        Email = employee.Email,
        Salary = employee.Salary,
        JoiningDate = employee.JoiningDate,
        DepartmentName = employee.Department.Name
    };

    /// <remarks>Reads <see cref="Project.Employees"/>; empty if it isn't loaded.</remarks>
    public static ProjectResponseDto ToResponseDto(this Project project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        Description = project.Description,
        EmployeeNames = project.Employees.Select(e => e.Name).ToList()
    };

    public static SkillResponseDto ToResponseDto(this Skill skill) => new()
    {
        Id = skill.Id,
        Name = skill.Name
    };

    /// <remarks>Requires <see cref="EmployeeSkill.Skill"/> to be loaded.</remarks>
    public static EmployeeSkillResponseDto ToResponseDto(this EmployeeSkill employeeSkill) => new()
    {
        EmployeeId = employeeSkill.EmployeeId,
        SkillId = employeeSkill.SkillId,
        SkillName = employeeSkill.Skill.Name,
        ProficiencyLevel = employeeSkill.ProficiencyLevel,
        YearsOfExperience = employeeSkill.YearsOfExperience
    };
}
