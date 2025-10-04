using FluentValidation;
using ModularMonolith.Roles.Services;

namespace ModularMonolith.Roles.Queries.GetRole;

/// <summary>
/// Validator for GetRoleQuery following the 3-file pattern with localized messages
/// </summary>
public sealed class GetRoleValidator : AbstractValidator<GetRoleQuery>
{
    public GetRoleValidator(IRoleLocalizationService roleLocalizationService)
    {
        RuleFor(x => x.RoleId)
            .NotEmpty()
            .WithMessage(roleLocalizationService.GetString("RoleIdRequired"))
            .NotEqual(Guid.Empty)
            .WithMessage(roleLocalizationService.GetString("RoleIdInvalid"));
    }
}