using FluentValidation;
using ModularMonolith.Users.Services;

namespace ModularMonolith.Users.Queries.GetUser;

/// <summary>
/// Validator for GetUserQuery following the 3-file pattern with localized messages
/// </summary>
public class GetUserValidator : AbstractValidator<GetUserQuery>
{
    public GetUserValidator(IUserLocalizationService userLocalizationService)
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage(userLocalizationService.GetString("UserIdRequired"))
            .NotEqual(Guid.Empty)
            .WithMessage(userLocalizationService.GetString("UserIdInvalid"));
    }
}