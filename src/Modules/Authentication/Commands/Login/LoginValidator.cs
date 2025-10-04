using FluentValidation;
using ModularMonolith.Authentication.Services;

namespace ModularMonolith.Authentication.Commands.Login;

/// <summary>
/// Validator for LoginCommand following the 3-file pattern with localized messages
/// </summary>
public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator(IAuthLocalizationService authLocalizationService)
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(authLocalizationService.GetString("EmailRequired"))
            .EmailAddress()
            .WithMessage(authLocalizationService.GetString("EmailInvalid"))
            .MaximumLength(255)
            .WithMessage(authLocalizationService.GetString("EmailMaxLength"));
            
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage(authLocalizationService.GetString("PasswordRequired"))
            .MinimumLength(1)
            .WithMessage(authLocalizationService.GetString("PasswordEmpty"));
    }
}