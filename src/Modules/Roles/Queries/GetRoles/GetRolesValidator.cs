using FluentValidation;

namespace ModularMonolith.Roles.Queries.GetRoles;

/// <summary>
/// Validator for GetRolesQuery following the 3-file pattern
/// </summary>
public sealed class GetRolesValidator : AbstractValidator<GetRolesQuery>
{
    public GetRolesValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size cannot exceed 100");

        RuleFor(x => x.NameFilter)
            .MaximumLength(100)
            .WithMessage("Name filter cannot exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.NameFilter));

        RuleFor(x => x.PermissionResource)
            .MaximumLength(100)
            .WithMessage("Permission resource filter cannot exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.PermissionResource));

        RuleFor(x => x.PermissionAction)
            .MaximumLength(50)
            .WithMessage("Permission action filter cannot exceed 50 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.PermissionAction));
    }
}