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
internal sealed class DeleteUserHandler : ICommandHandler<DeleteUserCommand, DeleteUserResponse>
{
    private readonly ILogger<DeleteUserHandler> _logger;
    private readonly IUserRepository _userRepository;
    private readonly ITimeService _timeService;
    private readonly IUserLocalizationService _userLocalizationService;
    
    public DeleteUserHandler(
        ILogger<DeleteUserHandler> logger,
        IUserRepository userRepository,
        ITimeService timeService,
        IUserLocalizationService userLocalizationService)
    {
        _logger = logger;
        _userRepository = userRepository;
        _timeService = timeService;
        _userLocalizationService = userLocalizationService;
    }
    
    public async Task<Result<DeleteUserResponse>> Handle(
        DeleteUserCommand command, 
        CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Command"] = nameof(DeleteUserCommand),
            ["UserId"] = command.Id,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        _logger.LogInformation("Deleting user with ID {UserId}", command.Id);
        
        try
        {
            // Check if user exists
            var userId = UserId.From(command.Id);
            var userExists = await _userRepository.ExistsAsync(userId, cancellationToken);
            
            if (!userExists)
            {
                _logger.LogWarning("User with ID {UserId} not found", command.Id);
                return Result<DeleteUserResponse>.Failure(
                    Error.NotFound("USER_NOT_FOUND", _userLocalizationService.GetString("UserNotFound")));
            }

            // Soft delete the user
            await _userRepository.SoftDeleteAsync(userId, cancellationToken);

            var response = new DeleteUserResponse(
                command.Id,
                _timeService.UtcNow
            );
            
            _logger.LogInformation("User deleted successfully with ID {UserId}", command.Id);
            
            return Result<DeleteUserResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user with ID {UserId}", command.Id);
            return Result<DeleteUserResponse>.Failure(
                Error.Internal("USER_DELETION_FAILED", _userLocalizationService.GetString("UserDeletionFailed")));
        }
    }
}
