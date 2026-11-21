using Npgsql;

namespace EmployeeManagement.Repositories.AdoNet;

/// <summary>
/// A custom DELEGATE type: a variable that holds "any method that takes a data reader positioned on
/// a row and returns a T". It is the same idea as the built-in Func&lt;NpgsqlDataReader, T&gt;,
/// written out by hand so the name says what it is for.
/// </summary>
/// <typeparam name="T">What one row is turned into.</typeparam>
public delegate T RowMapper<out T>(NpgsqlDataReader reader);