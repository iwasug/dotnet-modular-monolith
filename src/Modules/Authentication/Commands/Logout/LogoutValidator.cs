using FluentValidation;

namespace ModularMonolith.Authentication.Commands.Logout;

/// <summary>
/// Validator for LogoutCommand following the 3-file pattern
/// </summary>
public class LogoutValidator : AbstractValidator<LogoutCommand>
{
    public LogoutValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required")
            .MinimumLength(10)
            .WithMessage("Refresh token must be at least 10 characters long");
    }
}