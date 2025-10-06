using System.Text.Json.Serialization;

namespace ModularMonolith.Shared.Common;

/// <summary>
/// Standardized API response wrapper
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public sealed record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; init; }
    
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Error? Error { get; init; }

    private ApiResponse() { }

    public static ApiResponse<T> Ok(T data, string message = "Operation completed successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ApiResponse<T> Fail(string message, Error? error = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Message = null,
            Error = error,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ApiResponse<T> FromResult(Result<T> result, string? successMessage = null)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value!, successMessage ?? "Operation completed successfully");
        }

        return Fail(result.Error.Message, result.Error);
    }
}

/// <summary>
/// Non-generic API response for operations that don't return data
/// </summary>
public sealed record ApiResponse
{
    public bool Success { get; init; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; init; }
    
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Error? Error { get; init; }

    private ApiResponse() { }

    public static ApiResponse Ok(string message = "Operation completed successfully")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ApiResponse Fail(string message, Error? error = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = null,
            Error = error,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ApiResponse FromResult(Result result, string? successMessage = null)
    {
        if (result.IsSuccess)
        {
            return Ok(successMessage ?? "Operation completed successfully");
        }

        return Fail(result.Error.Message, result.Error);
    }
}
