using EmployeeManagement.Models;

namespace EmployeeManagement.Repositories.AdoNet.Interfaces;

public interface IEmployeeAdoRepository
{
    Task<List<Employee>> GetAllEmployeesAsync();
    Task<Employee?> GetEmployeeByIdAsync(int id);
}