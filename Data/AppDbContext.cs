using EmployeeManagement.Models;
using Microsoft.EntityFrameworkCore;
namespace EmployeeManagement.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    { }
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<EmployeeSkill> EmployeeSkills => Set<EmployeeSkill>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Employee.Email uses the case-insensitive "citext" type, which needs this extension.
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(150)
                .HasColumnType("citext");
            
            entity.HasIndex(e => e.Email)
                .IsUnique();
            
            entity.Property(e => e.Salary)
                .HasPrecision(18,2);
            
            entity.HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.JoiningDate)
                .HasColumnType("date")
                .HasDefaultValueSql("CURRENT_DATE");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(100);
        });
    
        /* 
        Implicit Join Entity for Employee-Project (instead of Explicit Join Entity, which would need a separate class 'EmployeeProject'). 
        */ 
        modelBuilder.Entity<Employee>()
            .HasMany(e => e.Projects)
            .WithMany(p => p.Employees)
            .UsingEntity<Dictionary<string,object>>(
                "EmployeeProjects",
                j => j
                    .HasOne<Project>()
                    .WithMany()
                    .HasForeignKey("ProjectId")
                    .OnDelete(DeleteBehavior.Restrict),
                j => j
                    .HasOne<Employee>()
                    .WithMany()
                    .HasForeignKey("EmployeeId")
                    .OnDelete(DeleteBehavior.Restrict),
                j =>
                {
                    j.HasKey("EmployeeId", "ProjectId");
                    j.ToTable("EmployeeProjects");
                }    
            );
        
        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(s => s.Id);
            
            entity.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.HasIndex(s => s.Name)
                .IsUnique();
        });
        /* 
        Explicit Join Entity for Employee-Skill relationship.
        */ 
        modelBuilder.Entity<EmployeeSkill>(entity =>
        {
            // Composite Primary Key
            entity.HasKey(es => new { es.EmployeeId, es.SkillId });

            // Employee -> EmployeeSkills (One-to-many)
            entity.HasOne(es => es.Employee)
                .WithMany(e => e.EmployeeSkills)
                .HasForeignKey(es => es.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Skill -> EmployeeSkills (One-to-Many)
            entity.HasOne(es => es.Skill)
                .WithMany(s => s.EmployeeSkills)
                .HasForeignKey(es => es.SkillId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // ProficiencyLevel configuration
            entity.Property(es => es.ProficiencyLevel)
                .IsRequired()
                .HasMaxLength(30);
            
            // YearsOfExperience configuration
            entity.ToTable("EmployeeSkills", table =>
            {
                table.HasCheckConstraint(
                    "CK_EmployeeSkills_YearsOfExperience",
                    "\"YearsOfExperience\" >= 0"
                );
            });
        });
    }
}