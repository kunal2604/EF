using EmployeeManagement.Models;
using EmployeeManagement.Repositories.AdoNet.Interfaces;
using EmployeeManagement.Services.Interfaces;

namespace EmployeeManagement.Services;

public class EmployeeAdoService : IEmployeeAdoService
{
    private readonly IEmployeeAdoRepository _repository;

    public EmployeeAdoService(IEmployeeAdoRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Employee>> GetAllEmployeesAsync()
    {
        return await _repository.GetAllEmployeesAsync();
    }

    public async Task<Employee?> GetEmployeeByIdAsync(int id)
    {
        return await _repository.GetEmployeeByIdAsync(id);
    }
}