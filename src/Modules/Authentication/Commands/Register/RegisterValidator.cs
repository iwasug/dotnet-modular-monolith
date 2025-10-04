using FluentValidation;
using ModularMonolith.Authentication.Services;

namespace ModularMonolith.Authentication.Commands.Register;

/// <summary>
/// Validator for RegisterCommand with localized messages
/// </summary>
public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator(IAuthLocalizationService authLocalizationService)
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
            .MinimumLength(8)
            .WithMessage(authLocalizationService.GetString("PasswordMinLength"))
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
            .WithMessage(authLocalizationService.GetString("PasswordComplexity"));
            
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage(authLocalizationService.GetString("PasswordMismatch"));
            
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage(authLocalizationService.GetString("FirstNameRequired"))
            .MaximumLength(100)
            .WithMessage(authLocalizationService.GetString("FirstNameMaxLength"));
            
        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage(authLocalizationService.GetString("LastNameRequired"))
            .MaximumLength(100)
            .WithMessage(authLocalizationService.GetString("LastNameMaxLength"));
    }
}
