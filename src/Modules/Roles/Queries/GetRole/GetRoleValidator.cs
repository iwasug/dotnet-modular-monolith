using FluentValidation;

namespace ModularMonolith.Roles.Queries.GetRole;

/// <summary>
/// Validator for GetRoleQuery following the 3-file pattern
/// </summary>
public sealed class GetRoleValidator : AbstractValidator<GetRoleQuery>
{
    public GetRoleValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty()
            .WithMessage("Role ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("Role ID must be a valid GUID");
    }
}