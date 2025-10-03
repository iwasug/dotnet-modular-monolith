using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Authentication.Commands.Login;

/// <summary>
/// Command to authenticate a user with email and password
/// </summary>
public record LoginCommand(
    string Email,
    string Password
) : ICommand<LoginResponse>;

/// <summary>
/// Response for user login
/// </summary>
public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string TokenType,
    Guid UserId,
    string Email
);