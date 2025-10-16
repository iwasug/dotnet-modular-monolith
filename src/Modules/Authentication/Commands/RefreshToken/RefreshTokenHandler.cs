using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Authentication.Domain.Services;
using ModularMonolith.Authentication.Services;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Authentication.Commands.RefreshToken;

/// <summary>
/// Handler for RefreshTokenCommand following the 3-file pattern
/// </summary>
public class RefreshTokenHandler(
    ILogger<RefreshTokenHandler> logger,
    IAuthenticationService authenticationService,
    IAuthLocalizationService authLocalizationService)
    : ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    public async Task<Result<RefreshTokenResponse>> Handle(
        RefreshTokenCommand command, 
        CancellationToken cancellationToken = default)
    {
        using var activity = logger.BeginScope(new Dictionary<string, object>
        {
            ["Command"] = nameof(RefreshTokenCommand),
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        logger.LogInformation("Refreshing authentication tokens");
        
        try
        {
            // Refresh tokens
            var authResult = await authenticationService.RefreshAsync(command.RefreshToken, cancellationToken);
            
            if (!authResult.IsSuccess)
            {
                logger.LogWarning("Token refresh failed: {ErrorCode}", authResult.ErrorCode);
                
                return authResult.ErrorCode switch
                {
                    "INVALID_REFRESH_TOKEN" => Result<RefreshTokenResponse>.Failure(
                        Error.Unauthorized("INVALID_REFRESH_TOKEN", authLocalizationService.GetString("RefreshTokenInvalid"))),
                    _ => Result<RefreshTokenResponse>.Failure(
                        Error.Internal("TOKEN_REFRESH_FAILED", authLocalizationService.GetString("RefreshTokenExpired")))
                };
            }

            var tokenResult = authResult.TokenResult!;
            
            var response = new RefreshTokenResponse(
                tokenResult.AccessToken,
                tokenResult.RefreshToken,
                tokenResult.ExpiresAt,
                tokenResult.TokenType
            );
            
            logger.LogInformation("Tokens refreshed successfully");
            
            return Result<RefreshTokenResponse>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing tokens");
            return Result<RefreshTokenResponse>.Failure(
                Error.Internal("TOKEN_REFRESH_ERROR", authLocalizationService.GetString("RefreshTokenInvalid")));
        }
    }
}