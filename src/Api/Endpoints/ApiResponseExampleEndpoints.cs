using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Extensions;
using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Api.Endpoints;

/// <summary>
/// Example endpoints demonstrating the standardized API response structure
/// </summary>
public sealed class ApiResponseExampleEndpoints : IEndpointModule
{
    public void MapEndpoints(WebApplication app)
    {
        var examples = app.MapGroup("/api/examples")
            .WithTags("API Response Examples")
            .WithOpenApi();

        // Example 1: Simple success response with data
        examples.MapGet("/success", () =>
        {
            var data = new { Id = 1, Name = "Example Item", Description = "This is an example" };
            return data.ToOkApiResponse("Item retrieved successfully");
        })
        .WithName("ExampleSuccess")
        .WithSummary("Example: Successful response with data")
        .Produces<ApiResponse<object>>();

        // Example 2: Simple success response without data
        examples.MapPost("/operation", () =>
        {
            return Results.Ok(ApiResponse.Ok("Operation completed successfully"));
        })
        .WithName("ExampleOperation")
        .WithSummary("Example: Successful operation without data")
        .Produces<ApiResponse>();

        // Example 3: Paginated response
        examples.MapGet("/paginated", (int page = 1, int limit = 20) =>
        {
            var items = Enumerable.Range(1, 100)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(i => new { Id = i, Name = $"Item {i}" })
                .ToList();

            var response = items.ToPagedApiResponse(page, limit, 100, "Items retrieved successfully");
            return Results.Ok(response);
        })
        .WithName("ExamplePaginated")
        .WithSummary("Example: Paginated response")
        .Produces<PagedApiResponse<object>>();

        // Example 4: Error response - Not Found
        examples.MapGet("/error/notfound", () =>
        {
            var error = Error.NotFound("ITEM_NOT_FOUND", "The requested item was not found");
            return ToFailedApiResponse<object>("Item not found", error);
        })
        .WithName("ExampleNotFound")
        .WithSummary("Example: Not found error response")
        .Produces<ApiResponse<object>>(404);

        // Example 5: Error response - Validation
        examples.MapPost("/error/validation", () =>
        {
            var error = Error.Validation("INVALID_INPUT", "The provided input is invalid");
            return ToFailedApiResponse<object>("Validation failed", error);
        })
        .WithName("ExampleValidation")
        .WithSummary("Example: Validation error response")
        .Produces<ApiResponse<object>>(400);

        // Example 6: Using Result<T> pattern
        examples.MapGet("/with-result/{id:int}", (int id) =>
        {
            Result<ExampleDto> result = id > 0
                ? Result<ExampleDto>.Success(new ExampleDto(id, $"Item {id}", DateTime.UtcNow))
                : Error.Validation("INVALID_ID", "ID must be greater than 0");

            return result.ToResult("Item retrieved successfully");
        })
        .WithName("ExampleWithResult")
        .WithSummary("Example: Using Result pattern")
        .Produces<ApiResponse<ExampleDto>>()
        .Produces<ApiResponse<ExampleDto>>(400);

        // Example 7: Complex response with metadata
        examples.MapGet("/complex", () =>
        {
            var data = new
            {
                Id = 1,
                Name = "Complex Item",
                Properties = new
                {
                    Color = "Blue",
                    Size = "Large",
                    Price = 99.99m
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return Results.Ok(ApiResponse<object>.Ok(data, "Complex data retrieved successfully"));
        })
        .WithName("ExampleComplex")
        .WithSummary("Example: Complex response with nested data")
        .Produces<ApiResponse<object>>();
    }

    private static IResult ToFailedApiResponse<T>(string message, Error? error = null)
    {
        return ApiResponseExtensions.ToFailedApiResponse<T>(message, error);
    }
}

public sealed record ExampleDto(int Id, string Name, DateTime CreatedAt);
