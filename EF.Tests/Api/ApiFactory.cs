using EmployeeManagement.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using NSubstitute.ClearExtensions;

namespace EF.Tests.Api;

/// <summary>
/// Starts the real app in memory (real routing, validation, JSON and exception handler)
/// with the service layer replaced by substitutes, so no database is needed.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public IEmployeeService Employees { get; } = Substitute.For<IEmployeeService>();
    public IDepartmentService Departments { get; } = Substitute.For<IDepartmentService>();
    public IProjectService Projects { get; } = Substitute.For<IProjectService>();
    public ISkillService Skills { get; } = Substitute.For<ISkillService>();
    public IEmployeeSkillService EmployeeSkills { get; } = Substitute.For<IEmployeeSkillService>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" keeps user secrets (your real connection string) out of the picture.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=unused;Database=unused;Username=u;Password=p");

        builder.ConfigureTestServices(services =>
        {
            Replace(services, Employees);
            Replace(services, Departments);
            Replace(services, Projects);
            Replace(services, Skills);
            Replace(services, EmployeeSkills);
        });
    }

    private static void Replace<T>(IServiceCollection services, T instance) where T : class
    {
        services.RemoveAll<T>();
        services.AddSingleton(instance);
    }

    public void ResetMocks()
    {
        foreach (var mock in new object[] { Employees, Departments, Projects, Skills, EmployeeSkills })
            mock.ClearSubstitute(ClearOptions.All);
    }
}
