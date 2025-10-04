using FluentValidation;
using ModularMonolith.Users.Services;

namespace ModularMonolith.Users.Commands.DeleteUser;

/// <summary>
/// Validator for DeleteUserCommand with localized messages
/// </summary>
public class DeleteUserValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserValidator(IUserLocalizationService userLocalizationService)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(userLocalizationService.GetString("UserIdRequired"));
    }
}
