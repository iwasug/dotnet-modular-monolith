using FluentValidation;
using ModularMonolith.Roles.Services;

namespace ModularMonolith.Roles.Commands.AssignRoleToUser;

/// <summary>
/// Validator for AssignRoleToUserCommand following the 3-file pattern with localized messages
/// </summary>
public sealed class AssignRoleToUserValidator : AbstractValidator<AssignRoleToUserCommand>
{
    public AssignRoleToUserValidator(IRoleLocalizationService roleLocalizationService)
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage(roleLocalizationService.GetString("UserIdRequired"))
            .NotEqual(Guid.Empty)
            .WithMessage(roleLocalizationService.GetString("UserIdInvalid"));

        RuleFor(x => x.RoleId)
            .NotEmpty()
            .WithMessage(roleLocalizationService.GetString("RoleIdRequired"))
            .NotEqual(Guid.Empty)
            .WithMessage(roleLocalizationService.GetString("RoleIdInvalid"));

        RuleFor(x => x.AssignedBy)
            .NotEqual(Guid.Empty)
            .WithMessage(roleLocalizationService.GetString("AssignedByInvalid"))
            .When(x => x.AssignedBy.HasValue);
    }
}