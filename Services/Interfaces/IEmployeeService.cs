using EmployeeManagement.DTOs.Employees;

namespace EmployeeManagement.Services.Interfaces;

public interface IEmployeeService
{
    Task<List<EmployeeResponseDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<EmployeeResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<EmployeeResponseDto> CreateAsync(CreateEmployeeDto dto, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(int id, UpdateEmployeeDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}