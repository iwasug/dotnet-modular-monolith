using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ModularMonolith.Authentication.Domain;
using ModularMonolith.Authentication.Domain.Services;
using ModularMonolith.Authentication.Domain.ValueObjects;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Users.Domain;
using ModularMonolith.Users.Domain.Services;
using ModularMonolith.Users.Domain.ValueObjects;

namespace ModularMonolith.Authentication.Infrastructure.Services;

/// <summary>
/// Authentication service implementation with login/logout operations and password verification
/// </summary>
internal sealed class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly Users.Domain.Services.IPasswordHashingService _passwordHashingService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITimeService _timeService;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IUserRepository userRepository,
        Users.Domain.Services.IPasswordHashingService passwordHashingService,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository,
        ITimeService timeService,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository;
        _passwordHashingService = passwordHashingService;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _timeService = timeService;
        _logger = logger;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = nameof(AuthenticateAsync),
            ["Email"] = email
        });

        _logger.LogInformation("Authenticating user with email {Email}", email);

        try
        {
            // Validate input parameters
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Authentication failed: Email is null or empty");
                return AuthenticationResult.InvalidCredentials();
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Authentication failed: Password is null or empty");
                return AuthenticationResult.InvalidCredentials();
            }

            // Find user by email
            var userEmail = Email.From(email);
            var user = await _userRepository.GetByEmailAsync(userEmail, cancellationToken);

            if (user is null)
            {
                _logger.LogWarning("Authentication failed: User not found with email {Email}", email);
                return AuthenticationResult.InvalidCredentials();
            }

            // Check if user is not deleted (soft delete check)
            if (user.IsDeleted)
            {
                _logger.LogWarning("Authentication failed: User {UserId} is deleted", user.Id);
                return AuthenticationResult.UserInactive();
            }

            // Verify password
            if (!_passwordHashingService.VerifyPassword(password, user.Password))
            {
                _logger.LogWarning("Authentication failed: Invalid password for user {UserId}", user.Id);
                return AuthenticationResult.InvalidCredentials();
            }

            // Update last login timestamp
            await UpdateLastLoginAsync(user.Id, cancellationToken);

            // Generate tokens
            var tokenResult = await _tokenService.GenerateTokensAsync(user, cancellationToken);

            _logger.LogInformation("Successfully authenticated user {UserId}", user.Id);
            return AuthenticationResult.Success(tokenResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for email {Email}", email);
            throw;
        }
    }

    public async Task<AuthenticationResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = nameof(RefreshAsync),
            ["RefreshToken"] = refreshToken[..Math.Min(refreshToken.Length, 10)] + "..."
        });

        _logger.LogInformation("Refreshing authentication tokens");

        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("Token refresh failed: Refresh token is null or empty");
                return AuthenticationResult.InvalidRefreshToken();
            }

            // Use token service to refresh tokens
            var tokenResult = await _tokenService.RefreshTokensAsync(refreshToken, cancellationToken);

            _logger.LogInformation("Successfully refreshed authentication tokens");
            return AuthenticationResult.Success(tokenResult);
        }
        catch (SecurityTokenValidationException ex)
        {
            _logger.LogWarning(ex, "Token refresh failed: Invalid refresh token");
            return AuthenticationResult.InvalidRefreshToken();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            throw;
        }
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = nameof(LogoutAsync),
            ["RefreshToken"] = refreshToken[..Math.Min(refreshToken.Length, 10)] + "..."
        });

        _logger.LogInformation("Logging out user");

        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("Logout failed: Refresh token is null or empty");
                return;
            }

            // Revoke the refresh token
            await _tokenService.RevokeTokensAsync(refreshToken, cancellationToken);

            _logger.LogInformation("Successfully logged out user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            throw;
        }
    }

    public async Task<bool> IsUserActiveAsync(string email, CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = nameof(IsUserActiveAsync),
            ["Email"] = email
        });

        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            var userEmail = Email.From(email);
            var user = await _userRepository.GetByEmailAsync(userEmail, cancellationToken);

            var isActive = user is not null && !user.IsDeleted;
            _logger.LogDebug("User {Email} active status: {IsActive}", email, isActive);

            return isActive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user active status for email {Email}", email);
            return false;
        }
    }

    public async Task UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = nameof(UpdateLastLoginAsync),
            ["UserId"] = userId
        });

        try
        {
            var userIdValue = UserId.From(userId);
            var user = await _userRepository.GetByIdAsync(userIdValue, cancellationToken);

            if (user is not null)
            {
                user.UpdateLastLogin();
                await _userRepository.UpdateAsync(user, cancellationToken);
                _logger.LogDebug("Updated last login timestamp for user {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Cannot update last login: User {UserId} not found", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login timestamp for user {UserId}", userId);
            throw;
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = nameof(RevokeAllUserTokensAsync),
            ["UserId"] = userId
        });

        _logger.LogInformation("Revoking all tokens for user {UserId}", userId);

        try
        {
            var userIdValue = UserId.From(userId);
            
            // Revoke all refresh tokens for the user
            await _refreshTokenRepository.RevokeAllForUserAsync(userIdValue, cancellationToken);
            
            _logger.LogInformation("Successfully revoked all tokens for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all tokens for user {UserId}", userId);
            throw;
        }
    }
}