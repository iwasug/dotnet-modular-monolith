using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Authentication.Domain.Services;
using ModularMonolith.Authentication.Services;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Authentication.Commands.RefreshToken;

/// <summary>
/// Handler for RefreshTokenCommand following the 3-file pattern
/// </summary>
public class RefreshTokenHandler : ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly ILogger<RefreshTokenHandler> _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly IAuthLocalizationService _authLocalizationService;
    
    public RefreshTokenHandler(
        ILogger<RefreshTokenHandler> logger,
        IAuthenticationService authenticationService,
        IAuthLocalizationService authLocalizationService)
    {
        _logger = logger;
        _authenticationService = authenticationService;
        _authLocalizationService = authLocalizationService;
    }
    
    public async Task<Result<RefreshTokenResponse>> Handle(
        RefreshTokenCommand command, 
        CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Command"] = nameof(RefreshTokenCommand),
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        _logger.LogInformation("Refreshing authentication tokens");
        
        try
        {
            // Refresh tokens
            var authResult = await _authenticationService.RefreshAsync(command.RefreshToken, cancellationToken);
            
            if (!authResult.IsSuccess)
            {
                _logger.LogWarning("Token refresh failed: {ErrorCode}", authResult.ErrorCode);
                
                return authResult.ErrorCode switch
                {
                    "INVALID_REFRESH_TOKEN" => Result<RefreshTokenResponse>.Failure(
                        Error.Unauthorized("INVALID_REFRESH_TOKEN", _authLocalizationService.GetString("RefreshTokenInvalid"))),
                    _ => Result<RefreshTokenResponse>.Failure(
                        Error.Internal("TOKEN_REFRESH_FAILED", _authLocalizationService.GetString("RefreshTokenExpired")))
                };
            }

            var tokenResult = authResult.TokenResult!;
            
            var response = new RefreshTokenResponse(
                tokenResult.AccessToken,
                tokenResult.RefreshToken,
                tokenResult.ExpiresAt,
                tokenResult.TokenType
            );
            
            _logger.LogInformation("Tokens refreshed successfully");
            
            return Result<RefreshTokenResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing tokens");
            return Result<RefreshTokenResponse>.Failure(
                Error.Internal("TOKEN_REFRESH_ERROR", _authLocalizationService.GetString("RefreshTokenInvalid")));
        }
    }
}