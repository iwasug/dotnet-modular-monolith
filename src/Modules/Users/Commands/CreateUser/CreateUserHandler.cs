using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Users.Domain;
using ModularMonolith.Users.Domain.ValueObjects;
using ModularMonolith.Users.Services;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Users.Commands.CreateUser;

/// <summary>
/// Handler for CreateUserCommand following the 3-file pattern with localized error messages
/// </summary>
internal sealed class CreateUserHandler(
    ILogger<CreateUserHandler> logger,
    IUserRepository userRepository,
    IPasswordHashingService passwordHashingService,
    ITimeService timeService,
    IUserLocalizationService userLocalizationService)
    : ICommandHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly ITimeService _timeService = timeService;

    public async Task<Result<CreateUserResponse>> Handle(
        CreateUserCommand command, 
        CancellationToken cancellationToken = default)
    {
        using var activity = logger.BeginScope(new Dictionary<string, object>
        {
            ["Command"] = nameof(CreateUserCommand),
            ["Email"] = command.Email,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        logger.LogInformation("Creating user with email {Email}", command.Email);
        
        try
        {
            // Check if user already exists
            Email email = Email.From(command.Email);
            User? existingUser = await userRepository.GetByEmailAsync(email, cancellationToken);
            if (existingUser is not null)
            {
                logger.LogWarning("User with email {Email} already exists", command.Email);
                return Result<CreateUserResponse>.Failure(
                    Error.Conflict("USER_ALREADY_EXISTS", userLocalizationService.GetString("UserAlreadyExists")));
            }

            // Hash password
            HashedPassword hashedPassword = passwordHashingService.HashPassword(command.Password);

            // Create user entity
            var user = User.Create(command.Email, hashedPassword, command.FirstName, command.LastName);

            // Save to repository - audit information will be set automatically by DbContext
            await userRepository.AddAsync(user, cancellationToken);

            var response = new CreateUserResponse(
                user.Id,
                user.Email.Value,
                user.Profile.FirstName,
                user.Profile.LastName,
                user.CreatedAt
            );
            
            logger.LogInformation("User created successfully with ID {UserId}", response.Id);
            
            return Result<CreateUserResponse>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating user with email {Email}", command.Email);
            return Result<CreateUserResponse>.Failure(
                Error.Internal("USER_CREATION_FAILED", userLocalizationService.GetString("UserCreationFailed")));
        }
    }
}