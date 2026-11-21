using Npgsql;
using EmployeeManagement.Models;
using EmployeeManagement.Repositories.AdoNet.Interfaces;

namespace EmployeeManagement.Repositories.AdoNet;

public class EmployeeAdoRepository : IEmployeeAdoRepository
{
    private readonly string _connectionString;

    // Shared by both queries; each one only adds its own WHERE clause.
    private const string SelectEmployees = """
        SELECT
            "Id",
            "Name",
            "Email",
            "Salary",
            "JoiningDate",
            "DepartmentId"
        FROM "Employees"
        """;

    public EmployeeAdoRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string not found.");
    }

    public async Task<List<Employee>> GetAllEmployeesAsync()
    {
        // MethodGroup -> delegate: MapEmployee is passed by name, with no parentheses and no lambda.
        // The compiler converts it to a RowMapper<Employee>.
        return await QueryAsync(SelectEmployees, MapEmployee);
    }

    public async Task<Employee?> GetEmployeeByIdAsync(int id)
    {
        // Lambda -> Action<NpgsqlParameterCollection>: this little piece of code is handed to
        // QueryAsync, which runs it later, once the command exists. It "closes over" the local `id`.
        var rows = await QueryAsync(
            SelectEmployees + """

            WHERE "Id" = @id
            """,
            MapEmployee,
            parameters => parameters.AddWithValue("id", id));

        return rows.FirstOrDefault();
    }

    /// <summary>
    /// The one place that opens the connection, runs a query and loops over the rows. What differs
    /// between queries is passed in as delegates instead of being copy-pasted:
    /// <paramref name="map"/> says how to turn a row into a T, and <paramref name="addParameters"/>
    /// (optional) fills in the command's @parameters.
    /// </summary>
    private async Task<List<T>> QueryAsync<T>(
        string sql,
        RowMapper<T> map,
        Action<NpgsqlParameterCollection>? addParameters = null)
    {
        var results = new List<T>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        addParameters?.Invoke(command.Parameters);     // run the delegate, only if one was given

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            results.Add(map(reader));                  // calling a delegate looks like calling a method

        return results;
    }

    /// <summary>Has the shape of <see cref="RowMapper{T}"/>, so it can be passed wherever one is expected.</summary>
    private static Employee MapEmployee(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt32(reader.GetOrdinal("Id")),
        Name = reader.GetString(reader.GetOrdinal("Name")),
        Email = reader.GetString(reader.GetOrdinal("Email")),
        Salary = reader.GetDecimal(reader.GetOrdinal("Salary")),
        JoiningDate = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("JoiningDate")),
        DepartmentId = reader.GetInt32(reader.GetOrdinal("DepartmentId"))
    };
}
