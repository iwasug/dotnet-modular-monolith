using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Authentication.Domain.Services;
using ModularMonolith.Authentication.Services;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Authentication.Commands.Logout;

/// <summary>
/// Handler for LogoutCommand following the 3-file pattern
/// </summary>
public class LogoutHandler : ICommandHandler<LogoutCommand, LogoutResponse>
{
    private readonly ILogger<LogoutHandler> _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly IAuthLocalizationService _authLocalizationService;
    
    public LogoutHandler(
        ILogger<LogoutHandler> logger,
        IAuthenticationService authenticationService,
        IAuthLocalizationService authLocalizationService)
    {
        _logger = logger;
        _authenticationService = authenticationService;
        _authLocalizationService = authLocalizationService;
    }
    
    public async Task<Result<LogoutResponse>> Handle(
        LogoutCommand command, 
        CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Command"] = nameof(LogoutCommand),
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        _logger.LogInformation("Logging out user");
        
        try
        {
            // Logout user by revoking refresh token
            await _authenticationService.LogoutAsync(command.RefreshToken, cancellationToken);
            
            var response = new LogoutResponse(true, "Logout successful");
            
            _logger.LogInformation("User logged out successfully");
            
            return Result<LogoutResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            
            // Even if logout fails, we should return success to the client
            // to prevent information leakage about token validity
            var response = new LogoutResponse(true, "Logout completed");
            return Result<LogoutResponse>.Success(response);
        }
    }
}