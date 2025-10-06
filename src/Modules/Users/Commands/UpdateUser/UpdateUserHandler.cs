using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Users.Domain;
using ModularMonolith.Users.Domain.ValueObjects;
using ModularMonolith.Users.Services;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Users.Commands.UpdateUser;

/// <summary>
/// Handler for UpdateUserCommand with localized error messages
/// </summary>
internal sealed class UpdateUserHandler : ICommandHandler<UpdateUserCommand, UpdateUserResponse>
{
    private readonly ILogger<UpdateUserHandler> _logger;
    private readonly IUserRepository _userRepository;
    private readonly ITimeService _timeService;
    private readonly IUserLocalizationService _userLocalizationService;
    
    public UpdateUserHandler(
        ILogger<UpdateUserHandler> logger,
        IUserRepository userRepository,
        ITimeService timeService,
        IUserLocalizationService userLocalizationService)
    {
        _logger = logger;
        _userRepository = userRepository;
        _timeService = timeService;
        _userLocalizationService = userLocalizationService;
    }
    
    public async Task<Result<UpdateUserResponse>> Handle(
        UpdateUserCommand command, 
        CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Command"] = nameof(UpdateUserCommand),
            ["UserId"] = command.Id,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        _logger.LogInformation("Updating user with ID {UserId}", command.Id);
        
        try
        {
            // Get existing user
            UserId userId = UserId.From(command.Id);
            User? user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            
            if (user is null)
            {
                _logger.LogWarning("User with ID {UserId} not found", command.Id);
                return Result<UpdateUserResponse>.Failure(
                    Error.NotFound("USER_NOT_FOUND", _userLocalizationService.GetString("UserNotFound")));
            }

            // Check if email is being changed and if new email already exists
            Email newEmail = Email.From(command.Email);
            if (user.Email != newEmail)
            {
                User? existingUserWithEmail = await _userRepository.GetByEmailAsync(newEmail, cancellationToken);
                if (existingUserWithEmail is not null && existingUserWithEmail.Id != command.Id)
                {
                    _logger.LogWarning("Email {Email} already exists for another user", command.Email);
                    return Result<UpdateUserResponse>.Failure(
                        Error.Conflict("USER_ALREADY_EXISTS", _userLocalizationService.GetString("UserAlreadyExists")));
                }
            }

            // Update user profile
            UserProfile newProfile = UserProfile.Create(command.FirstName, command.LastName);
            user.UpdateProfile(newProfile);

            // Update repository
            await _userRepository.UpdateAsync(user, cancellationToken);

            var response = new UpdateUserResponse(
                user.Id,
                user.Email.Value,
                user.Profile.FirstName,
                user.Profile.LastName,
                user.UpdatedAt
            );
            
            _logger.LogInformation("User updated successfully with ID {UserId}", response.Id);
            
            return Result<UpdateUserResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user with ID {UserId}", command.Id);
            return Result<UpdateUserResponse>.Failure(
                Error.Internal("USER_UPDATE_FAILED", _userLocalizationService.GetString("UserUpdateFailed")));
        }
    }
}
