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
            .SetValidator(new PermissionDtoValidator())
            .When(x => x.Permissions is not null);
    }
}

/// <summary>
/// Validator for PermissionDto
/// </summary>
public sealed class PermissionDtoValidator : AbstractValidator<PermissionDto>
{
    public PermissionDtoValidator()
    {
        RuleFor(x => x.Resource)
            .NotEmpty()
            .WithMessage("Resource is required")
            .MaximumLength(100)
            .WithMessage("Resource must not exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\-_]+$")
            .WithMessage("Resource can only contain letters, numbers, hyphens, and underscores");

        RuleFor(x => x.Action)
            .NotEmpty()
            .WithMessage("Action is required")
            .MaximumLength(50)
            .WithMessage("Action must not exceed 50 characters")
            .Matches(@"^[a-zA-Z0-9\-_]+$")
            .WithMessage("Action can only contain letters, numbers, hyphens, and underscores");

        RuleFor(x => x.Scope)
            .NotEmpty()
            .WithMessage("Scope is required")
            .MaximumLength(50)
            .WithMessage("Scope must not exceed 50 characters")
            .Matches(@"^[a-zA-Z0-9\-_*]+$")
            .WithMessage("Scope can only contain letters, numbers, hyphens, underscores, and asterisks");
    }
}