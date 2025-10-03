using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Authentication.Commands.Logout;

/// <summary>
/// Command to logout a user by revoking their refresh token
/// </summary>
public record LogoutCommand(
    string RefreshToken
) : ICommand<LogoutResponse>;

/// <summary>
/// Response for user logout
/// </summary>
public record LogoutResponse(
    bool Success,
    string Message
);