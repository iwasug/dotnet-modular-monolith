using FluentValidation;
using ModularMonolith.Roles.Services;

namespace ModularMonolith.Roles.Commands.CreateRole;

/// <summary>
/// Validator for CreateRoleCommand following the 3-file pattern with localized messages
/// </summary>
public sealed class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleValidator(IRoleLocalizationService roleLocalizationService)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(roleLocalizationService.GetString("RoleNameRequired"))
            .MaximumLength(100)
            .WithMessage(roleLocalizationService.GetString("RoleNameMaxLength"))
            .Matches(@"^[a-zA-Z0-9\s\-_]+$")
            .WithMessage("Role name can only contain letters, numbers, spaces, hyphens, and underscores");
            
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.Permissions)
            .NotNull()
            .WithMessage(roleLocalizationService.GetString("PermissionsRequired"));

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