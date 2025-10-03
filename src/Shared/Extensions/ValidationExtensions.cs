using FluentValidation;
using FluentValidation.Results;

namespace ModularMonolith.Shared.Extensions;

/// <summary>
/// Extension methods for FluentValidation integration
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Converts FluentValidation results to dictionary format for API responses
    /// </summary>
    public static IDictionary<string, string[]> ToDictionary(this ValidationResult validationResult)
    {
        return validationResult.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.ErrorMessage).ToArray()
            );
    }

    /// <summary>
    /// Validates a request and returns validation errors if any
    /// </summary>
    public static async Task<ValidationResult> ValidateAsync<T>(
        this IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken = default)
    {
        return await validator.ValidateAsync(instance, cancellationToken);
    }
}