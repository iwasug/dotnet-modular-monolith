using FluentValidation;
using ModularMonolith.Authentication.Services;

namespace ModularMonolith.Authentication.Commands.RefreshToken;

/// <summary>
/// Validator for RefreshTokenCommand following the 3-file pattern with localized messages
/// </summary>
public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator(IAuthLocalizationService authLocalizationService)
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage(authLocalizationService.GetString("RefreshTokenRequired"))
            .MinimumLength(10)
            .WithMessage(authLocalizationService.GetString("RefreshTokenMinLength"));
    }
}