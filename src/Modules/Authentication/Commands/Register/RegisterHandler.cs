using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Authentication.Services;
using ModularMonolith.Users.Commands.CreateUser;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Authentication.Commands.Register;

/// <summary>
/// Handler for RegisterCommand
/// </summary>
public class RegisterHandler(
    ILogger<RegisterHandler> logger,
    ICommandHandler<CreateUserCommand, CreateUserResponse> createUserHandler,
    IAuthLocalizationService authLocalizationService)
    : ICommandHandler<RegisterCommand, RegisterResponse>
{
    public async Task<Result<RegisterResponse>> Handle(
        RegisterCommand command, 
        CancellationToken cancellationToken = default)
    {
        using var activity = logger.BeginScope(new Dictionary<string, object>
        {
            ["Command"] = nameof(RegisterCommand),
            ["Email"] = command.Email,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        logger.LogInformation("Registering new user with email {Email}", command.Email);
        
        try
        {
            var createUserCommand = new CreateUserCommand(
                command.Email,
                command.Password,
                command.FirstName,
                command.LastName
            );
            
            var result = await createUserHandler.Handle(createUserCommand, cancellationToken);
            
            if (!result.IsSuccess)
            {
                logger.LogWarning("User registration failed for email {Email}: {Error}", 
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
                authLocalizationService.GetString("RegistrationSuccessful")
            );
            
            logger.LogInformation("User registered successfully with email {Email}, UserId {UserId}", 
                command.Email, userResponse.Id);
            
            return Result<RegisterResponse>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering user with email {Email}", command.Email);
            return Result<RegisterResponse>.Failure(
                Error.Internal("REGISTRATION_ERROR", authLocalizationService.GetString("RegistrationFailed")));
        }
    }
}
