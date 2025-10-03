using FluentValidation;

namespace ModularMonolith.Roles.Commands.AssignRoleToUser;

/// <summary>
/// Validator for AssignRoleToUserCommand following the 3-file pattern
/// </summary>
public sealed class AssignRoleToUserValidator : AbstractValidator<AssignRoleToUserCommand>
{
    public AssignRoleToUserValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("User ID must be a valid GUID");

        RuleFor(x => x.RoleId)
            .NotEmpty()
            .WithMessage("Role ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("Role ID must be a valid GUID");

        RuleFor(x => x.AssignedBy)
            .NotEqual(Guid.Empty)
            .WithMessage("AssignedBy must be a valid GUID when provided")
            .When(x => x.AssignedBy.HasValue);
    }
}