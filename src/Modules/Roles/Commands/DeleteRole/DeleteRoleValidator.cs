using FluentValidation;
using ModularMonolith.Roles.Services;

namespace ModularMonolith.Roles.Commands.DeleteRole;

/// <summary>
/// Validator for DeleteRoleCommand with localized messages
/// </summary>
public sealed class DeleteRoleValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleValidator(IRoleLocalizationService roleLocalizationService)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(roleLocalizationService.GetString("RoleIdRequired"));
    }
}
