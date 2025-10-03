using FluentValidation;

namespace ModularMonolith.Authentication.Commands.RefreshToken;

/// <summary>
/// Validator for RefreshTokenCommand following the 3-file pattern
/// </summary>
public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required")
            .MinimumLength(10)
            .WithMessage("Refresh token must be at least 10 characters long");
    }
}