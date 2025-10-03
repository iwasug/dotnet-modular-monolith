using ModularMonolith.Authentication.Commands.Login;
using ModularMonolith.Authentication.Commands.RefreshToken;
using ModularMonolith.Authentication.Commands.Logout;
using Swashbuckle.AspNetCore.Filters;

namespace ModularMonolith.Api.Examples;

/// <summary>
/// Example provider for login command
/// </summary>
public sealed class LoginCommandExample : IExamplesProvider<LoginCommand>
{
    public LoginCommand GetExamples()
    {
        return new LoginCommand(
            Email: "admin@example.com",
            Password: "SecurePassword123!"
        );
    }
}

/// <summary>
/// Example provider for login response
/// </summary>
public sealed class LoginResponseExample : IExamplesProvider<LoginResponse>
{
    public LoginResponse GetExamples()
    {
        return new LoginResponse(
            AccessToken: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
            RefreshToken: "def50200a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef",
            ExpiresAt: DateTime.UtcNow.AddMinutes(15),
            TokenType: "Bearer",
            UserId: Guid.Parse("12345678-1234-1234-1234-123456789012"),
            Email: "admin@example.com"
        );
    }
}

/// <summary>
/// Example provider for refresh token command
/// </summary>
public sealed class RefreshTokenCommandExample : IExamplesProvider<RefreshTokenCommand>
{
    public RefreshTokenCommand GetExamples()
    {
        return new RefreshTokenCommand(
            RefreshToken: "def50200a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef"
        );
    }
}

/// <summary>
/// Example provider for refresh token response
/// </summary>
public sealed class RefreshTokenResponseExample : IExamplesProvider<RefreshTokenResponse>
{
    public RefreshTokenResponse GetExamples()
    {
        return new RefreshTokenResponse(
            AccessToken: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
            RefreshToken: "abc12300a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef",
            ExpiresAt: DateTime.UtcNow.AddMinutes(15),
            TokenType: "Bearer"
        );
    }
}

/// <summary>
/// Example provider for logout command
/// </summary>
public sealed class LogoutCommandExample : IExamplesProvider<LogoutCommand>
{
    public LogoutCommand GetExamples()
    {
        return new LogoutCommand(
            RefreshToken: "def50200a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef"
        );
    }
}

/// <summary>
/// Example provider for logout response
/// </summary>
public sealed class LogoutResponseExample : IExamplesProvider<LogoutResponse>
{
    public LogoutResponse GetExamples()
    {
        return new LogoutResponse(
            Success: true,
            Message: "Logout completed successfully"
        );
    }
}