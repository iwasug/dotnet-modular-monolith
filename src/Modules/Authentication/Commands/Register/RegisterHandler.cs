using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Authentication.Services;
using ModularMonolith.Users.Commands.CreateUser;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Authentication.Commands.Register;

/// <summary>
/// Handler for RegisterCommand
/// </summary>
public class RegisterHandler : ICommandHandler<RegisterCommand, RegisterResponse>
{
    private readonly ILogger<RegisterHandler> _logger;
    private readonly ICommandHandler<CreateUserCommand, CreateUserResponse> _createUserHandler;
    private readonly IAuthLocalizationService _authLocalizationService;
    
    public RegisterHandler(
        ILogger<RegisterHandler> logger,
        ICommandHandler<CreateUserCommand, CreateUserResponse> createUserHandler,
        IAuthLocalizationService authLocalizationService)
    {
        _logger = logger;
        _createUserHandler = createUserHandler;
        _authLocalizationService = authLocalizationService;
    }
    
    public async Task<Result<RegisterResponse>> Handle(
        RegisterCommand command, 
        CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Command"] = nameof(RegisterCommand),
            ["Email"] = command.Email,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        _logger.LogInformation("Registering new user with email {Email}", command.Email);
        
        try
        {
            var createUserCommand = new CreateUserCommand(
                command.Email,
                command.Password,
                command.FirstName,
                command.LastName
            );
            
            var result = await _createUserHandler.Handle(createUserCommand, cancellationToken);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("User registration failed for email {Email}: {Error}", 
                    command.Email, result.Error?.Message);
                return Result<RegisterResponse>.Failure(result.Error!);
            }
            
            var userResponse = result.Value!;
            var response = new RegisterResponse(
                userResponse.Id,
                userResponse.Email,
                userResponse.FirstName,
                userResponse.LastName,
                userResponse.CreatedAt,
                _authLocalizationService.GetString("RegistrationSuccessful")
            );
            
            _logger.LogInformation("User registered successfully with email {Email}, UserId {UserId}", 
                command.Email, userResponse.Id);
            
            return Result<RegisterResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user with email {Email}", command.Email);
            return Result<RegisterResponse>.Failure(
                Error.Internal("REGISTRATION_ERROR", _authLocalizationService.GetString("RegistrationFailed")));
        }
    }
}
