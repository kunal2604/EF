using EmployeeManagement.Data;
using EmployeeManagement.Exceptions;
using EmployeeManagement.Repositories;
using EmployeeManagement.Repositories.Interfaces;
using EmployeeManagement.Services;
using EmployeeManagement.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers
builder.Services.AddControllers();

// Global error handling (RFC 7807 problem details)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// OpenAPI document generation (the Swagger UI is added below, Development only)
builder.Services.AddOpenApi();

// Configure PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Register repositories
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<ISkillRepository, SkillRepository>();
builder.Services.AddScoped<IEmployeeSkillRepository, EmployeeSkillRepository>();

// Register services
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IEmployeeSkillService, EmployeeSkillService>();
builder.Services.AddScoped<ISkillService, SkillService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // Serves the OpenAPI document at /openapi/v1.json
    app.MapOpenApi();

    // Swagger UI at /swagger, reading that document
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Employee Management API v1");
        options.RoutePrefix = "swagger";
    });
}

app.MapControllers();

app.Run();