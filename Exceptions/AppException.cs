namespace EmployeeManagement.Exceptions;

/// <summary>
/// Base type for expected, client-facing failures. The global exception
/// handler maps each subclass to an HTTP status code.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message, int statusCode, string title) : base(message)
    {
        StatusCode = statusCode;
        Title = title;
    }

    public int StatusCode { get; }
    public string Title { get; }
}

/// <summary>The request is invalid (HTTP 400).</summary>
public class BadRequestException(string message)
    : AppException(message, StatusCodes.Status400BadRequest, "Bad request");

/// <summary>A requested resource does not exist (HTTP 404).</summary>
public class NotFoundException(string message)
    : AppException(message, StatusCodes.Status404NotFound, "Not found");

/// <summary>The request conflicts with current state (HTTP 409).</summary>
public class ConflictException(string message)
    : AppException(message, StatusCodes.Status409Conflict, "Conflict");
