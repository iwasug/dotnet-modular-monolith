using FluentValidation;
using ModularMonolith.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ModularMonolith.Shared.Extensions;

/// <summary>
/// Extension methods for adding localized validation messages to FluentValidation
/// </summary>
public static class LocalizedValidationExtensions
{
    /// <summary>
    /// Adds localized validation message using the localization service
    /// </summary>
    public static IRuleBuilderOptions<T, TProperty> WithLocalizedMessage<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule,
        string messageKey,
        ILocalizationService localizationService)
    {
        return rule.WithMessage(localizationService.GetString(messageKey));
    }

    /// <summary>
    /// Adds localized validation message with format arguments
    /// </summary>
    public static IRuleBuilderOptions<T, TProperty> WithLocalizedMessage<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule,
        string messageKey,
        ILocalizationService localizationService,
        params object[] args)
    {
        return rule.WithMessage(localizationService.GetString(messageKey, args));
    }
}

/// <summary>
/// Sets up localized validation for common rules
/// </summary>
public static class LocalizedRules
{
    public static IRuleBuilderOptions<T, string> NotEmptyWithLocalizedMessage<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        string fieldName,
        ILocalizationService localizationService)
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage(localizationService.GetString("Required", fieldName));
    }

    public static IRuleBuilderOptions<T, string> EmailAddressWithLocalizedMessage<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        ILocalizationService localizationService)
    {
        return ruleBuilder
            .EmailAddress()
            .WithMessage(localizationService.GetString("EmailInvalid"));
    }

    public static IRuleBuilderOptions<T, string> MaximumLengthWithLocalizedMessage<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        int maxLength,
        string fieldName,
        ILocalizationService localizationService)
    {
        return ruleBuilder
            .MaximumLength(maxLength)
            .WithMessage(localizationService.GetString("MaxLength", fieldName, maxLength));
    }

    public static IRuleBuilderOptions<T, string> MinimumLengthWithLocalizedMessage<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        int minLength,
        string fieldName,
        ILocalizationService localizationService)
    {
        return ruleBuilder
            .MinimumLength(minLength)
            .WithMessage(localizationService.GetString("MinLength", fieldName, minLength));
    }

    public static IRuleBuilderOptions<T, Guid> NotEmptyGuidWithLocalizedMessage<T>(
        this IRuleBuilder<T, Guid> ruleBuilder,
        string fieldName,
        ILocalizationService localizationService)
    {
        return ruleBuilder
            .NotEmpty()
            .NotEqual(Guid.Empty)
            .WithMessage(localizationService.GetString("InvalidGuid", fieldName));
    }
}