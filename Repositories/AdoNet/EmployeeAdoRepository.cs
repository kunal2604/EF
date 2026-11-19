using Npgsql;
using EmployeeManagement.Models;
using EmployeeManagement.Repositories.AdoNet.Interfaces;

namespace EmployeeManagement.Repositories.AdoNet;

public class EmployeeAdoRepository : IEmployeeAdoRepository
{
    private readonly string _connectionString;

    public EmployeeAdoRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string not found.");
    }

    public async Task<List<Employee>> GetAllEmployeesAsync()
    {
        var employees = new List<Employee>();

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync();

        const string query = """
            SELECT
                "Id",
                "Name",
                "Email",
                "Salary",
                "JoiningDate",
                "DepartmentId"
            FROM "Employees"
            """;

        await using var command =
            new NpgsqlCommand(query, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var employee = new Employee
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                Salary = reader.GetDecimal(reader.GetOrdinal("Salary")),
                JoiningDate = reader.GetFieldValue<DateOnly>(
                    reader.GetOrdinal("JoiningDate")),
                DepartmentId = reader.GetInt32(
                    reader.GetOrdinal("DepartmentId"))
            };

            employees.Add(employee);
        }

        return employees;
    }

    public async Task<Employee?> GetEmployeeByIdAsync(int id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        
        await connection.OpenAsync();

        const string query = """
            SELECT 
                "Id",
                "Name",
                "Email",
                "Salary",
                "JoiningDate",
                "DepartmentId"
            FROM "Employees"
            WHERE "Id" = @id
        """;

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync();

        if(await reader.ReadAsync())
        {
            return new Employee
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                Salary = reader.GetDecimal(reader.GetOrdinal("Salary")),
                JoiningDate = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("JoiningDate")),
                DepartmentId = reader.GetInt32(reader.GetOrdinal("DepartmentId"))
            };
        }

        return null;
    }
}