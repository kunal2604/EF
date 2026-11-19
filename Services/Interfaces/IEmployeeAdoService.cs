using EmployeeManagement.Models;

namespace EmployeeManagement.Services.Interfaces;

public interface IEmployeeAdoService
{
    Task<List<Employee>> GetAllEmployeesAsync();
    Task<Employee?> GetEmployeeByIdAsync(int id);
}