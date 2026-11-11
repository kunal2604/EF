using System.Text.Json;
using EmployeeManagement.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace EF.Tests.Exceptions;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _sut = new(NullLogger<GlobalExceptionHandler>.Instance);

    private static DefaultHttpContext NewContext(string method = "POST")
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = method;
        ctx.Request.Path = "/api/test";
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static async Task<JsonElement?> BodyAsync(HttpContext ctx)
    {
        ctx.Response.Body.Position = 0;
        if (ctx.Response.Body.Length == 0) return null;
        return await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Response.Body);
    }

    private static DbUpdateException Db(string sqlState, string? constraint) =>
        new("save failed", new PostgresException(
            messageText: "db error", severity: "ERROR", invariantSeverity: "ERROR",
            sqlState: sqlState, constraintName: constraint));

    private async Task<(int Status, string? Detail)> Handle(Exception ex, string method = "POST")
    {
        var ctx = NewContext(method);
        var handled = await _sut.TryHandleAsync(ctx, ex, CancellationToken.None);
        Assert.True(handled);
        var body = await BodyAsync(ctx);
        string? detail = body is { } b && b.TryGetProperty("detail", out var d) ? d.GetString() : null;
        return (ctx.Response.StatusCode, detail);
    }

    // ---- application exceptions ----

    [Theory]
    [InlineData(typeof(NotFoundException), 404)]
    [InlineData(typeof(ConflictException), 409)]
    [InlineData(typeof(BadRequestException), 400)]
    public async Task App_exceptions_map_to_their_status_and_keep_the_message(Type type, int status)
    {
        var ex = (Exception)Activator.CreateInstance(type, "the message")!;

        var (actual, detail) = await Handle(ex);

        Assert.Equal(status, actual);
        Assert.Equal("the message", detail);
    }

    // ---- unexpected ----

    [Fact]
    public async Task Unexpected_exceptions_return_500_without_leaking_details()
    {
        var ctx = NewContext();

        await _sut.TryHandleAsync(ctx, new InvalidOperationException("connection string password=hunter2"), CancellationToken.None);

        ctx.Response.Body.Position = 0;
        var text = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
        Assert.Equal(500, ctx.Response.StatusCode);
        Assert.DoesNotContain("hunter2", text);
        Assert.DoesNotContain("InvalidOperation", text);
    }

    [Fact]
    public async Task A_plain_InvalidOperationException_is_not_mistaken_for_a_conflict()
    {
        // Regression: this type used to be mapped to "email already exists".
        var (status, _) = await Handle(new InvalidOperationException("anything"));

        Assert.Equal(500, status);
    }

    // ---- cancellation ----

    [Fact]
    public async Task Client_disconnect_is_499_with_no_body()
    {
        var ctx = NewContext("GET");
        using var cts = new CancellationTokenSource();
        ctx.RequestAborted = cts.Token;
        cts.Cancel();

        var handled = await _sut.TryHandleAsync(ctx, new OperationCanceledException(), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(499, ctx.Response.StatusCode);
        Assert.Equal(0, ctx.Response.Body.Length);
    }

    [Fact]
    public async Task A_cancellation_the_client_did_not_cause_is_still_an_error()
    {
        // e.g. an internal timeout: RequestAborted is NOT cancelled.
        var (status, _) = await Handle(new OperationCanceledException(), "GET");

        Assert.Equal(500, status);
    }

    // ---- database errors ----

    [Theory]
    [InlineData("IX_Employees_Email", "Email already exists !")]
    [InlineData("IX_Departments_Name", "Department already exists !")]
    [InlineData("IX_Skills_Name", "Skill already exists.")]
    [InlineData("PK_EmployeeSkills", "This skill is already assigned to the employee.")]
    [InlineData("PK_EmployeeProjects", "Employee is already assigned to this project !")]
    [InlineData("IX_Something_Else", "A record with the same unique value already exists.")]
    public async Task Unique_violation_23505_is_409_with_a_specific_message(string constraint, string expected)
    {
        var (status, detail) = await Handle(Db(PostgresErrorCodes.UniqueViolation, constraint));

        Assert.Equal(409, status);
        Assert.Equal(expected, detail);
    }

    [Theory]
    [InlineData("FK_EmployeeSkills_Employees_EmployeeId", "skills are assigned")]
    [InlineData("FK_EmployeeProjects_Employees_EmployeeId", "assigned to projects")]
    [InlineData("FK_EmployeeSkills_Skills_SkillId", "employees have it assigned")]
    [InlineData("FK_EmployeeProjects_Projects_ProjectId", "employees are assigned to it")]
    [InlineData("FK_Employees_Departments_DepartmentId", "employees belong to it")]
    public async Task Restrict_violation_23001_on_delete_is_409_naming_the_blocker(string constraint, string expectedFragment)
    {
        var (status, detail) = await Handle(Db(PostgresErrorCodes.RestrictViolation, constraint), "DELETE");

        Assert.Equal(409, status);
        Assert.Contains(expectedFragment, detail);
    }

    [Fact]
    public async Task Foreign_key_violation_23503_is_also_409()
    {
        var (status, _) = await Handle(Db(PostgresErrorCodes.ForeignKeyViolation, "FK_Employees_Departments_DepartmentId"), "DELETE");

        Assert.Equal(409, status);
    }

    [Fact]
    public async Task Foreign_key_violation_on_a_write_says_the_referenced_record_is_gone()
    {
        var (status, detail) = await Handle(Db(PostgresErrorCodes.ForeignKeyViolation, "FK_Unknown"), "POST");

        Assert.Equal(409, status);
        Assert.Contains("doesn't exist", detail);
    }

    [Fact]
    public async Task Unknown_database_errors_stay_500()
    {
        var (status, _) = await Handle(Db(PostgresErrorCodes.SerializationFailure, null));

        Assert.Equal(500, status);
    }
}
