using EmployeeManagement.DTOs.Departments;

namespace EmployeeManagement.Services.Interfaces;

public interface IDepartmentService
{
    Task<List<DepartmentResponseDto>> GetAllAsync();

    Task<DepartmentResponseDto> CreateAsync(CreateDepartmentDto dto);
}