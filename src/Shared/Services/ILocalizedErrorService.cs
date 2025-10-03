using ModularMonolith.Shared.Common;

namespace ModularMonolith.Shared.Services;

/// <summary>
/// Service for providing localized error messages
/// </summary>
public interface ILocalizedErrorService
{
    /// <summary>
    /// Creates a localized validation error
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="messageKey">Resource key for the error message</param>
    /// <param name="culture">Optional culture, uses current culture if not specified</param>
    /// <returns>Localized error</returns>
    Error CreateValidationError(string code, string messageKey, string? culture = null);

    /// <summary>
    /// Creates a localized validation error with format arguments
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="messageKey">Resource key for the error message</param>
    /// <param name="args">Format arguments</param>
    /// <returns>Localized error</returns>
    Error CreateValidationError(string code, string messageKey, params object[] args);

    /// <summary>
    /// Creates a localized validation error with culture and format arguments
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="messageKey">Resource key for the error message</param>
    /// <param name="culture">Culture to use</param>
    /// <param name="args">Format arguments</param>
    /// <returns>Localized error</returns>
    Error CreateValidationError(string code, string messageKey, string? culture, params object[] args);

    /// <summary>
    /// Creates a localized not found error
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="messageKey">Resource key for the error message</param>
    /// <param name="culture">Optional culture, uses current culture if not specified</param>
    /// <returns>Localized error</returns>
    Error CreateNotFoundError(string code, string messageKey, string? culture = null);

    /// <summary>
    /// Creates a localized conflict error
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="messageKey">Resource key for the error message</param>
    /// <param name="culture">Optional culture, uses current culture if not specified</param>
    /// <returns>Localized error</returns>
    Error CreateConflictError(string code, string messageKey, string? culture = null);

    /// <summary>
    /// Creates a localized unauthorized error
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="messageKey">Resource key for the error message</param>
    /// <param name="culture">Optional culture, uses current culture if not specified</param>
    /// <returns>Localized error</returns>
    Error CreateUnauthorizedError(string code, string messageKey, string? culture = null);

    /// <summary>
    /// Creates a localized forbidden error
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="messageKey">Resource key for the error message</param>
    /// <param name="culture">Optional culture, uses current culture if not specified</param>
    /// <returns>Localized error</returns>
    Error CreateForbiddenError(string code, string messageKey, string? culture = null);

    /// <summary>
    /// Creates a localized internal error
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="messageKey">Resource key for the error message</param>
    /// <param name="culture">Optional culture, uses current culture if not specified</param>
    /// <returns>Localized error</returns>
    Error CreateInternalError(string code, string messageKey, string? culture = null);
}