using FluentValidation;
using ModularMonolith.Roles.Services;

namespace ModularMonolith.Roles.Commands.UpdateRole;

/// <summary>
/// Validator for UpdateRoleCommand following the 3-file pattern with localized messages
/// </summary>
public sealed class UpdateRoleValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleValidator(IRoleLocalizationService roleLocalizationService)
    {
        RuleFor(x => x.RoleId)
            .NotEmpty()
            .WithMessage(roleLocalizationService.GetString("RoleIdRequired"))
            .NotEqual(Guid.Empty)
            .WithMessage(roleLocalizationService.GetString("RoleIdInvalid"));

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(roleLocalizationService.GetString("RoleNameRequired"))
            .MaximumLength(100)
            .WithMessage(roleLocalizationService.GetString("RoleNameMaxLength"))
            .Matches(@"^[a-zA-Z0-9\s\-_]+$")
            .WithMessage(roleLocalizationService.GetString("RoleNamePattern"));
            
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage(roleLocalizationService.GetString("DescriptionRequired"))
            .MaximumLength(500)
            .WithMessage(roleLocalizationService.GetString("DescriptionMaxLength"));

        RuleFor(x => x.Permissions)
            .NotNull()
            .WithMessage(roleLocalizationService.GetString("PermissionsListNull"));

        RuleForEach(x => x.Permissions)
            .SetValidator(new PermissionDtoValidator(roleLocalizationService))
            .When(x => x.Permissions is not null);
    }
}

/// <summary>
/// Validator for PermissionDto
/// </summary>
public sealed class PermissionDtoValidator : AbstractValidator<PermissionDto>
{
    public PermissionDtoValidator(IRoleLocalizationService roleLocalizationService)
    {
        RuleFor(x => x.Resource)
            .NotEmpty()
            .WithMessage(roleLocalizationService.GetString("ResourceRequired"))
            .MaximumLength(100)
            .WithMessage(roleLocalizationService.GetString("ResourceMaxLength"))
            .Matches(@"^[a-zA-Z0-9\-_]+$")
            .WithMessage(roleLocalizationService.GetString("ResourcePattern"));

        RuleFor(x => x.Action)
            .NotEmpty()
            .WithMessage(roleLocalizationService.GetString("ActionRequired"))
            .MaximumLength(50)
            .WithMessage(roleLocalizationService.GetString("ActionMaxLength"))
            .Matches(@"^[a-zA-Z0-9\-_]+$")
            .WithMessage(roleLocalizationService.GetString("ActionPattern"));

        RuleFor(x => x.Scope)
            .NotEmpty()
            .WithMessage(roleLocalizationService.GetString("ScopeRequired"))
            .MaximumLength(50)
            .WithMessage(roleLocalizationService.GetString("ScopeMaxLength"))
            .Matches(@"^[a-zA-Z0-9\-_*]+$")
            .WithMessage(roleLocalizationService.GetString("ScopePattern"));
    }
}