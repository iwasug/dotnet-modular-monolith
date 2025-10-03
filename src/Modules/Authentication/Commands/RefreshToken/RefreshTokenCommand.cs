using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Authentication.Commands.RefreshToken;

/// <summary>
/// Command to refresh authentication tokens
/// </summary>
public record RefreshTokenCommand(
    string RefreshToken
) : ICommand<RefreshTokenResponse>;

/// <summary>
/// Response for token refresh
/// </summary>
public record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string TokenType
);