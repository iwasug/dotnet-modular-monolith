using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ModularMonolith.Authentication.Domain;
using ModularMonolith.Authentication.Domain.Services;
using ModularMonolith.Authentication.Domain.ValueObjects;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Users.Domain;
using ModularMonolith.Users.Domain.ValueObjects;

namespace ModularMonolith.Authentication.Infrastructure.Services;

/// <summary>
/// JWT token service implementation with token generation, validation, and refresh token rotation
/// </summary>
internal sealed class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITimeService _timeService;
    private readonly ILogger<TokenService> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public TokenService(
        IConfiguration configuration,
        IRefreshTokenRepository refreshTokenRepository,
        ITimeService timeService,
        ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _refreshTokenRepository = refreshTokenRepository;
        _timeService = timeService;
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();
        _tokenValidationParameters = CreateTokenValidationParameters();
    }

    public async Task<TokenResult> GenerateTokensAsync(User user, CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = nameof(GenerateTokensAsync),
            ["UserId"] = user.Id,
            ["Email"] = user.Email.Value
        });

        _logger.LogInformation("Generating tokens for user {UserId}", user.Id);

        try
        {
            // Generate access token
            var accessToken = GenerateAccessToken(user);
            var accessTokenExpiry = _timeService.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes());

            // Generate refresh token
            var refreshTokenValue = GenerateRefreshTokenValue();
            var refreshTokenExpiry = _timeService.UtcNow.AddDays(GetRefreshTokenExpirationDays());

            // Create and store refresh token entity
            var refreshToken = RefreshToken.Create(
                UserId.From(user.Id),
                refreshTokenValue,
                refreshTokenExpiry);

            await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

            var tokenResult = TokenResult.Create(accessToken, refreshTokenValue, accessTokenExpiry);

            _logger.LogInformation("Successfully generated tokens for user {UserId}", user.Id);
            return tokenResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tokens for user {UserId}", user.Id);
            throw;
        }
    }

    public async Task<TokenResult> RefreshTokensAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = nameof(RefreshTokensAsync),
            ["RefreshToken"] = refreshToken[..Math.Min(refreshToken.Length, 10)] + "..."
        });

        _logger.LogInformation("Refreshing tokens using refresh token");

        try
        {
            // Validate refresh token
            var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken);
            if (storedToken is null || !storedToken.IsValid)
            {
                _logger.LogWarning("Invalid or expired refresh token provided");
                throw new SecurityTokenValidationException("Invalid or expired refresh token");
            }

            // Revoke the old refresh token (token rotation)
            storedToken.Revoke();
            await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);

            // For token refresh, we need the user information
            // Since we don't have direct access to user repository here, we'll need to get it through DI
            // For now, we'll create a minimal user representation from the stored token
            var userId = storedToken.UserId;

            // Generate new tokens
            var newAccessToken = GenerateAccessTokenForUserId(userId);
            var accessTokenExpiry = _timeService.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes());

            // Generate new refresh token
            var newRefreshTokenValue = GenerateRefreshTokenValue();
            var refreshTokenExpiry = _timeService.UtcNow.AddDays(GetRefreshTokenExpirationDays());

            // Create and store new refresh token entity
            var newRefreshToken = RefreshToken.Create(userId, newRefreshTokenValue, refreshTokenExpiry);
            await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

            var tokenResult = TokenResult.Create(newAccessToken, newRefreshTokenValue, accessTokenExpiry);

            _logger.LogInformation("Successfully refreshed tokens for user {UserId}", userId.Value);
            return tokenResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing tokens");
            throw;
        }
    }

    public async Task RevokeTokensAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = nameof(RevokeTokensAsync),
            ["RefreshToken"] = refreshToken[..Math.Min(refreshToken.Length, 10)] + "..."
        });

        _logger.LogInformation("Revoking refresh token");

        try
        {
            await _refreshTokenRepository.RevokeAsync(refreshToken, cancellationToken);
            _logger.LogInformation("Successfully revoked refresh token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token");
            throw;
        }
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var principal = _tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);
            
            // Ensure the token is a JWT token
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Token validation failed");
            return null;
        }
    }

    public Guid? GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        if (principal is null)
        {
            return null;
        }

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }

    public bool IsTokenExpired(string token)
    {
        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo <= _timeService.UtcNow;
        }
        catch
        {
            return true; // If we can't read the token, consider it expired
        }
    }

    private string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email.Value),
            new(ClaimTypes.Name, $"{user.Profile.FirstName} {user.Profile.LastName}"),
            new("jti", Guid.NewGuid().ToString()), // JWT ID for token uniqueness
            new("iat", new DateTimeOffset(_timeService.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add role claims if user has roles
        // Note: Role navigation properties need to be loaded separately
        // For now, we'll generate basic claims and enhance this when roles are loaded
        foreach (var userRole in user.Roles)
        {
            // Basic role claim using RoleId for now
            claims.Add(new Claim("role_id", userRole.RoleId.Value.ToString()));
        }

        return GenerateJwtToken(claims);
    }

    private string GenerateAccessTokenForUserId(UserId userId)
    {
        // Minimal token generation for refresh scenarios
        // In a real implementation, you might want to fetch user details from repository
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.Value.ToString()),
            new("jti", Guid.NewGuid().ToString()),
            new("iat", new DateTimeOffset(_timeService.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        return GenerateJwtToken(claims);
    }

    private string GenerateJwtToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSecretKey()));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = _timeService.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
            Issuer = GetIssuer(),
            Audience = GetAudience(),
            SigningCredentials = credentials
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    private static string GenerateRefreshTokenValue()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private TokenValidationParameters CreateTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSecretKey())),
            ValidateIssuer = true,
            ValidIssuer = GetIssuer(),
            ValidateAudience = true,
            ValidAudience = GetAudience(),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // Remove default 5-minute clock skew
        };
    }

    private string GetSecretKey()
    {
        var secretKey = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
        {
            throw new InvalidOperationException("JWT secret key must be at least 32 characters long");
        }
        return secretKey;
    }

    private string GetIssuer() => _configuration["Jwt:Issuer"] ?? "ModularMonolith";
    private string GetAudience() => _configuration["Jwt:Audience"] ?? "ModularMonolith";
    private int GetAccessTokenExpirationMinutes() => _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 15);
    private int GetRefreshTokenExpirationDays() => _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);
}