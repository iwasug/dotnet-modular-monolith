using ModularMonolith.Shared.Common;

namespace ModularMonolith.Shared.Services;

/// <summary>
/// Implementation of localized error service
/// </summary>
public sealed class LocalizedErrorService : ILocalizedErrorService
{
    private readonly ILocalizationService _localizationService;

    public LocalizedErrorService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public Error CreateValidationError(string code, string messageKey, string? culture = null)
    {
        var message = _localizationService.GetString(messageKey, culture);
        return Error.Validation(code, message);
    }

    public Error CreateValidationError(string code, string messageKey, params object[] args)
    {
        var message = _localizationService.GetString(messageKey, args);
        return Error.Validation(code, message);
    }

    public Error CreateValidationError(string code, string messageKey, string? culture, params object[] args)
    {
        var message = _localizationService.GetString(messageKey, culture, args);
        return Error.Validation(code, message);
    }

    public Error CreateNotFoundError(string code, string messageKey, string? culture = null)
    {
        var message = _localizationService.GetString(messageKey, culture);
        return Error.NotFound(code, message);
    }

    public Error CreateConflictError(string code, string messageKey, string? culture = null)
    {
        var message = _localizationService.GetString(messageKey, culture);
        return Error.Conflict(code, message);
    }

    public Error CreateUnauthorizedError(string code, string messageKey, string? culture = null)
    {
        var message = _localizationService.GetString(messageKey, culture);
        return Error.Unauthorized(code, message);
    }

    public Error CreateForbiddenError(string code, string messageKey, string? culture = null)
    {
        var message = _localizationService.GetString(messageKey, culture);
        return Error.Forbidden(code, message);
    }

    public Error CreateInternalError(string code, string messageKey, string? culture = null)
    {
        var message = _localizationService.GetString(messageKey, culture);
        return Error.Internal(code, message);
    }
}