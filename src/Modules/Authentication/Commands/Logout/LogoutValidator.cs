using FluentValidation;
using ModularMonolith.Authentication.Services;

namespace ModularMonolith.Authentication.Commands.Logout;

/// <summary>
/// Validator for LogoutCommand following the 3-file pattern with localized messages
/// </summary>
public class LogoutValidator : AbstractValidator<LogoutCommand>
{
    public LogoutValidator(IAuthLocalizationService authLocalizationService)
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage(authLocalizationService.GetString("RefreshTokenRequired"))
            .MinimumLength(10)
            .WithMessage(authLocalizationService.GetString("RefreshTokenMinLength"));
    }
}