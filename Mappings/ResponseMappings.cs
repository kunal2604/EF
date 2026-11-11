using System.Linq.Expressions;
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

    // The three mappings below are expressions, so EF Core can translate them into SQL and
    // load only the columns the DTO needs (query.Select(EmployeeProjection)). The
    // ToResponseDto() methods reuse the same expression for entities already in memory,
    // so a new field is still added in one place.

    /// <summary>Needs the department's name: used in a query it joins it, in memory it must be loaded.</summary>
    public static readonly Expression<Func<Employee, EmployeeResponseDto>> EmployeeProjection = employee => new EmployeeResponseDto
    {
        Id = employee.Id,
        Name = employee.Name,
        Email = employee.Email,
        Salary = employee.Salary,
        JoiningDate = employee.JoiningDate,
        DepartmentName = employee.Department.Name
    };
    
    /// <summary>Collects the employee names; in memory, empty if <see cref="Project.Employees"/> isn't loaded.</summary>
    public static readonly Expression<Func<Project, ProjectResponseDto>> ProjectProjection = project => new ProjectResponseDto
    {
        Id = project.Id,
        Name = project.Name,
        Description = project.Description,
        EmployeeNames = project.Employees.Select(e => e.Name).ToList()
    };

    /// <summary>Needs the skill's name: used in a query it joins it, in memory it must be loaded.</summary>
    public static readonly Expression<Func<EmployeeSkill, EmployeeSkillResponseDto>> EmployeeSkillProjection = employeeSkill => new EmployeeSkillResponseDto
    {
        EmployeeId = employeeSkill.EmployeeId,
        SkillId = employeeSkill.SkillId,
        SkillName = employeeSkill.Skill.Name,
        ProficiencyLevel = employeeSkill.ProficiencyLevel,
        YearsOfExperience = employeeSkill.YearsOfExperience
    };

    private static readonly Func<Employee, EmployeeResponseDto> EmployeeMapper = EmployeeProjection.Compile();
    private static readonly Func<Project, ProjectResponseDto> ProjectMapper = ProjectProjection.Compile();
    private static readonly Func<EmployeeSkill, EmployeeSkillResponseDto> EmployeeSkillMapper = EmployeeSkillProjection.Compile();

    /// <remarks>Requires <see cref="Employee.Department"/> to be loaded.</remarks>
    public static EmployeeResponseDto ToResponseDto(this Employee employee) => EmployeeMapper(employee);

    /// <remarks>Reads <see cref="Project.Employees"/>; empty if it isn't loaded.</remarks>
    public static ProjectResponseDto ToResponseDto(this Project project) => ProjectMapper(project);

    public static SkillResponseDto ToResponseDto(this Skill skill) => new()
    {
        Id = skill.Id,
        Name = skill.Name
    };

    /// <remarks>Requires <see cref="EmployeeSkill.Skill"/> to be loaded.</remarks>
    public static EmployeeSkillResponseDto ToResponseDto(this EmployeeSkill employeeSkill) => EmployeeSkillMapper(employeeSkill);
}
