using FluentValidation;
using ModularMonolith.Roles.Services;

namespace ModularMonolith.Roles.Queries.GetUserRoles;

/// <summary>
/// Validator for GetUserRolesQuery following the 3-file pattern with localized messages
/// </summary>
public sealed class GetUserRolesValidator : AbstractValidator<GetUserRolesQuery>
{
    public GetUserRolesValidator(IRoleLocalizationService roleLocalizationService)
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage(roleLocalizationService.GetString("UserIdRequired"))
            .NotEqual(Guid.Empty)
            .WithMessage(roleLocalizationService.GetString("UserIdInvalid"));
    }
}