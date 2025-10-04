using FluentValidation;
using ModularMonolith.Users.Services;

namespace ModularMonolith.Users.Commands.UpdateUser;

/// <summary>
/// Validator for UpdateUserCommand with localized messages
/// </summary>
public class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator(IUserLocalizationService userLocalizationService)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(userLocalizationService.GetString("UserIdRequired"));
            
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(userLocalizationService.GetString("EmailRequired"))
            .EmailAddress()
            .WithMessage(userLocalizationService.GetString("EmailInvalid"))
            .MaximumLength(255)
            .WithMessage(userLocalizationService.GetString("EmailMaxLength"));
            
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
