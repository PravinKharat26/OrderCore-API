using System.Text.Json.Serialization;

namespace IdentityService.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse OkResult(string message = "Success") =>
        new() { Success = true, Message = message };

    public static ApiResponse FailResult(string message, List<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}

public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message, 404) { }
}

public class ValidationException : AppException
{
    public List<string> ValidationErrors { get; }

    public ValidationException(List<string> errors) : base("One or more validation errors occurred.", 400)
    {
        ValidationErrors = errors;
    }
}
