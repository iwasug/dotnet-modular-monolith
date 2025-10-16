using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Users.Domain;
using ModularMonolith.Users.Domain.ValueObjects;
using ModularMonolith.Users.Services;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Users.Commands.DeleteUser;

/// <summary>
/// Handler for DeleteUserCommand with localized error messages
/// </summary>
internal sealed class DeleteUserHandler(
    ILogger<DeleteUserHandler> logger,
    IUserRepository userRepository,
    ITimeService timeService,
    IUserLocalizationService userLocalizationService)
    : ICommandHandler<DeleteUserCommand, DeleteUserResponse>
{
    public async Task<Result<DeleteUserResponse>> Handle(
        DeleteUserCommand command, 
        CancellationToken cancellationToken = default)
    {
        using var activity = logger.BeginScope(new Dictionary<string, object>
        {
            ["Command"] = nameof(DeleteUserCommand),
            ["UserId"] = command.Id,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        logger.LogInformation("Deleting user with ID {UserId}", command.Id);
        
        try
        {
            // Check if user exists
            var userId = UserId.From(command.Id);
            var userExists = await userRepository.ExistsAsync(userId, cancellationToken);
            
            if (!userExists)
            {
                logger.LogWarning("User with ID {UserId} not found", command.Id);
                return Result<DeleteUserResponse>.Failure(
                    Error.NotFound("USER_NOT_FOUND", userLocalizationService.GetString("UserNotFound")));
            }

            // Soft delete the user
            await userRepository.SoftDeleteAsync(userId, cancellationToken);

            var response = new DeleteUserResponse(
                command.Id,
                timeService.UtcNow
            );
            
            logger.LogInformation("User deleted successfully with ID {UserId}", command.Id);
            
            return Result<DeleteUserResponse>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting user with ID {UserId}", command.Id);
            return Result<DeleteUserResponse>.Failure(
                Error.Internal("USER_DELETION_FAILED", userLocalizationService.GetString("UserDeletionFailed")));
        }
    }
}
