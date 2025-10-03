using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Authentication.Domain.Services;
using ModularMonolith.Authentication.Services;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Authentication.Commands.Login;

/// <summary>
/// Handler for LoginCommand following the 3-file pattern
/// </summary>
public class LoginHandler : ICommandHandler<LoginCommand, LoginResponse>
{
    private readonly ILogger<LoginHandler> _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly IAuthLocalizationService _authLocalizationService;
    
    public LoginHandler(
        ILogger<LoginHandler> logger,
        IAuthenticationService authenticationService,
        IAuthLocalizationService authLocalizationService)
    {
        _logger = logger;
        _authenticationService = authenticationService;
        _authLocalizationService = authLocalizationService;
    }
    
    public async Task<Result<LoginResponse>> Handle(
        LoginCommand command, 
        CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Command"] = nameof(LoginCommand),
            ["Email"] = command.Email,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        _logger.LogInformation("Authenticating user with email {Email}", command.Email);
        
        try
        {
            // Authenticate user
            var authResult = await _authenticationService.AuthenticateAsync(command.Email, command.Password, cancellationToken);
            
            if (!authResult.IsSuccess)
            {
                _logger.LogWarning("Authentication failed for user {Email}: {ErrorCode}", command.Email, authResult.ErrorCode);
                
                return authResult.ErrorCode switch
                {
                    "INVALID_CREDENTIALS" => Result<LoginResponse>.Failure(
                        Error.Unauthorized("INVALID_CREDENTIALS", _authLocalizationService.GetString("InvalidCredentials"))),
                    "USER_INACTIVE" => Result<LoginResponse>.Failure(
                        Error.Forbidden("USER_INACTIVE", _authLocalizationService.GetString("AccountDisabled"))),
                    _ => Result<LoginResponse>.Failure(
                        Error.Internal("AUTHENTICATION_FAILED", _authLocalizationService.GetString("LoginFailed")))
                };
            }

            var tokenResult = authResult.TokenResult!;
            
            // Extract user ID from token (assuming it's in the token)
            var userId = Guid.NewGuid(); // TODO: Extract from token or get from user lookup
            
            var response = new LoginResponse(
                tokenResult.AccessToken,
                tokenResult.RefreshToken,
                tokenResult.ExpiresAt,
                tokenResult.TokenType,
                userId,
                command.Email
            );
            
            _logger.LogInformation("User authenticated successfully with email {Email}", command.Email);
            
            return Result<LoginResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating user with email {Email}", command.Email);
            return Result<LoginResponse>.Failure(
                Error.Internal("AUTHENTICATION_ERROR", _authLocalizationService.GetString("LoginFailed")));
        }
    }
}