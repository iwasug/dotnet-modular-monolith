using ModularMonolith.Shared.Common;
using Microsoft.AspNetCore.Http;

namespace ModularMonolith.Shared.Extensions;

/// <summary>
/// Extension methods for converting Result to ApiResponse and IResult
/// </summary>
public static class ApiResponseExtensions
{
    /// <summary>
    /// Converts a Result&lt;T&gt; to ApiResponse&lt;T&gt;
    /// </summary>
    public static ApiResponse<T> ToApiResponse<T>(this Result<T> result, string? successMessage = null)
    {
        return ApiResponse<T>.FromResult(result, successMessage);
    }

    /// <summary>
    /// Converts a Result to ApiResponse
    /// </summary>
    public static ApiResponse ToApiResponse(this Result result, string? successMessage = null)
    {
        return ApiResponse.FromResult(result, successMessage);
    }

    /// <summary>
    /// Converts a Result&lt;T&gt; to IResult for endpoint responses
    /// </summary>
    public static IResult ToResult<T>(this Result<T> result, string? successMessage = null)
    {
        var apiResponse = result.ToApiResponse(successMessage);
        
        if (apiResponse.Success)
        {
            return Results.Ok(apiResponse);
        }

        return apiResponse.Error?.Type switch
        {
            ErrorType.NotFound => Results.NotFound(apiResponse),
            ErrorType.Validation => Results.BadRequest(apiResponse),
            ErrorType.Unauthorized => Results.Json(apiResponse, statusCode: StatusCodes.Status401Unauthorized),
            ErrorType.Forbidden => Results.Json(apiResponse, statusCode: StatusCodes.Status403Forbidden),
            ErrorType.Conflict => Results.Conflict(apiResponse),
            _ => Results.Json(apiResponse, statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    /// <summary>
    /// Converts a Result to IResult for endpoint responses
    /// </summary>
    public static IResult ToResult(this Result result, string? successMessage = null)
    {
        var apiResponse = result.ToApiResponse(successMessage);
        
        if (apiResponse.Success)
        {
            return Results.Ok(apiResponse);
        }

        return apiResponse.Error?.Type switch
        {
            ErrorType.NotFound => Results.NotFound(apiResponse),
            ErrorType.Validation => Results.BadRequest(apiResponse),
            ErrorType.Unauthorized => Results.Json(apiResponse, statusCode: StatusCodes.Status401Unauthorized),
            ErrorType.Forbidden => Results.Json(apiResponse, statusCode: StatusCodes.Status403Forbidden),
            ErrorType.Conflict => Results.Conflict(apiResponse),
            _ => Results.Json(apiResponse, statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    /// <summary>
    /// Creates a successful paginated API response
    /// </summary>
    public static PagedApiResponse<T> ToPagedApiResponse<T>(
        this IReadOnlyList<T> data,
        int page,
        int limit,
        int total,
        string message = "Operation completed successfully")
    {
        return PagedApiResponse<T>.Ok(data, page, limit, total, message);
    }

    /// <summary>
    /// Creates a successful API response with data
    /// </summary>
    public static IResult ToOkApiResponse<T>(this T data, string message = "Operation completed successfully")
    {
        return Results.Ok(ApiResponse<T>.Ok(data, message));
    }

    /// <summary>
    /// Creates a failed API response
    /// </summary>
    public static IResult ToFailedApiResponse<T>(string message, Error? error = null)
    {
        var apiResponse = ApiResponse<T>.Fail(message, error);
        
        return error?.Type switch
        {
            ErrorType.NotFound => Results.NotFound(apiResponse),
            ErrorType.Validation => Results.BadRequest(apiResponse),
            ErrorType.Unauthorized => Results.Json(apiResponse, statusCode: StatusCodes.Status401Unauthorized),
            ErrorType.Forbidden => Results.Json(apiResponse, statusCode: StatusCodes.Status403Forbidden),
            ErrorType.Conflict => Results.Conflict(apiResponse),
            _ => Results.Json(apiResponse, statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
