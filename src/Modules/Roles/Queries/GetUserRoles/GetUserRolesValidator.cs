using FluentValidation;

namespace ModularMonolith.Roles.Queries.GetUserRoles;

/// <summary>
/// Validator for GetUserRolesQuery following the 3-file pattern
/// </summary>
public sealed class GetUserRolesValidator : AbstractValidator<GetUserRolesQuery>
{
    public GetUserRolesValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("User ID must be a valid GUID");
    }
}