using System.Collections.Generic;

namespace CRManager.Shared.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> Ok(T data, string message = "Success") => new()
    {
        Success = true,
        Message = message,
        Data = data
    };

    public static ApiResponse<T> Fail(string error, string message = "Failed") => new()
    {
        Success = false,
        Message = message,
        Errors = new List<string> { error }
    };

    public static ApiResponse<T> SuccessResponse(T data, string message = "Success") => Ok(data, message);
    public static ApiResponse<T> FailureResponse(string error, string message = "Failed") => Fail(error, message);
}
