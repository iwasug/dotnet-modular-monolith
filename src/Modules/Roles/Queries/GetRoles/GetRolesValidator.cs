using FluentValidation;
using ModularMonolith.Roles.Services;

namespace ModularMonolith.Roles.Queries.GetRoles;

/// <summary>
/// Validator for GetRolesQuery following the 3-file pattern with localized messages
/// </summary>
public sealed class GetRolesValidator : AbstractValidator<GetRolesQuery>
{
    public GetRolesValidator(IRoleLocalizationService roleLocalizationService)
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage(roleLocalizationService.GetString("PageNumberInvalid"));

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage(roleLocalizationService.GetString("PageSizeInvalid"))
            .LessThanOrEqualTo(100)
            .WithMessage(roleLocalizationService.GetString("PageSizeExceeded"));

        RuleFor(x => x.NameFilter)
            .MaximumLength(100)
            .WithMessage(roleLocalizationService.GetString("NameFilterMaxLength"))
            .When(x => !string.IsNullOrWhiteSpace(x.NameFilter));

        RuleFor(x => x.PermissionResource)
            .MaximumLength(100)
            .WithMessage(roleLocalizationService.GetString("PermissionResourceMaxLength"))
            .When(x => !string.IsNullOrWhiteSpace(x.PermissionResource));

        RuleFor(x => x.PermissionAction)
            .MaximumLength(50)
            .WithMessage(roleLocalizationService.GetString("PermissionActionMaxLength"))
            .When(x => !string.IsNullOrWhiteSpace(x.PermissionAction));
    }
}