using FluentValidation;

namespace ModularMonolith.Users.Queries.GetUser;

/// <summary>
/// Validator for GetUserQuery following the 3-file pattern
/// </summary>
public class GetUserValidator : AbstractValidator<GetUserQuery>
{
    public GetUserValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("User ID must be a valid GUID");
    }
}