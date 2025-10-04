using FluentValidation;
using ModularMonolith.Users.Services;

namespace ModularMonolith.Users.Commands.CreateUser;

/// <summary>
/// Validator for CreateUserCommand following the 3-file pattern with localized messages
/// </summary>
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator(IUserLocalizationService userLocalizationService)
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(userLocalizationService.GetString("EmailRequired"))
            .EmailAddress()
            .WithMessage(userLocalizationService.GetString("EmailInvalid"))
            .MaximumLength(255)
            .WithMessage(userLocalizationService.GetString("EmailMaxLength"));
            
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage(userLocalizationService.GetString("PasswordRequired"))
            .MinimumLength(8)
            .WithMessage(userLocalizationService.GetString("PasswordMinLength"))
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
            .WithMessage(userLocalizationService.GetString("PasswordComplexity"));
            
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage(userLocalizationService.GetString("FirstNameRequired"))
            .MaximumLength(100)
            .WithMessage(userLocalizationService.GetString("FirstNameMaxLength"))
            .Matches(@"^[a-zA-Z\s'-]+$")
            .WithMessage(userLocalizationService.GetString("FirstNamePattern"));
            
        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage(userLocalizationService.GetString("LastNameRequired"))
            .MaximumLength(100)
            .WithMessage(userLocalizationService.GetString("LastNameMaxLength"))
            .Matches(@"^[a-zA-Z\s'-]+$")
            .WithMessage(userLocalizationService.GetString("LastNamePattern"));
    }
}