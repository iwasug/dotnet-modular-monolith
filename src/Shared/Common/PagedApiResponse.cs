using System.Text.Json.Serialization;

namespace ModularMonolith.Shared.Common;

/// <summary>
/// Standardized paginated API response wrapper
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public sealed record PagedApiResponse<T>
{
    public bool Success { get; init; }
    public IReadOnlyList<T> Data { get; init; } = Array.Empty<T>();
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; init; }
    
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public PaginationMeta Meta { get; init; } = new();
    public Error? Error { get; init; }

    private PagedApiResponse() { }

    public static PagedApiResponse<T> Ok(
        IReadOnlyList<T> data,
        int page,
        int limit,
        int total,
        string message = "Operation completed successfully")
    {
        return new PagedApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Timestamp = DateTime.UtcNow,
            Meta = new PaginationMeta
            {
                Page = page,
                Limit = limit,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)limit)
            }
        };
    }

    public static PagedApiResponse<T> Fail(string message, Error? error = null)
    {
        return new PagedApiResponse<T>
        {
            Success = false,
            Data = Array.Empty<T>(),
            Message = null,
            Error = error,
            Timestamp = DateTime.UtcNow,
            Meta = new PaginationMeta()
        };
    }
}

/// <summary>
/// Pagination metadata
/// </summary>
public sealed record PaginationMeta
{
    public int Page { get; init; } = 1;
    public int Limit { get; init; } = 20;
    public int Total { get; init; } = 0;
    public int TotalPages { get; init; } = 0;
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
