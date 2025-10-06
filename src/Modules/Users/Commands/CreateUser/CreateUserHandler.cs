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
internal sealed class CreateUserHandler : ICommandHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly ILogger<CreateUserHandler> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly ITimeService _timeService;
    private readonly IUserLocalizationService _userLocalizationService;
    
    public CreateUserHandler(
        ILogger<CreateUserHandler> logger,
        IUserRepository userRepository,
        IPasswordHashingService passwordHashingService,
        ITimeService timeService,
        IUserLocalizationService userLocalizationService)
    {
        _logger = logger;
        _userRepository = userRepository;
        _passwordHashingService = passwordHashingService;
        _timeService = timeService;
        _userLocalizationService = userLocalizationService;
    }
    
    public async Task<Result<CreateUserResponse>> Handle(
        CreateUserCommand command, 
        CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Command"] = nameof(CreateUserCommand),
            ["Email"] = command.Email,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        _logger.LogInformation("Creating user with email {Email}", command.Email);
        
        try
        {
            // Check if user already exists
            Email email = Email.From(command.Email);
            User? existingUser = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (existingUser is not null)
            {
                _logger.LogWarning("User with email {Email} already exists", command.Email);
                return Result<CreateUserResponse>.Failure(
                    Error.Conflict("USER_ALREADY_EXISTS", _userLocalizationService.GetString("UserAlreadyExists")));
            }

            // Hash password
            HashedPassword hashedPassword = _passwordHashingService.HashPassword(command.Password);

            // Create user entity
            var user = User.Create(command.Email, hashedPassword, command.FirstName, command.LastName);

            // Save to repository - audit information will be set automatically by DbContext
            await _userRepository.AddAsync(user, cancellationToken);

            var response = new CreateUserResponse(
                user.Id,
                user.Email.Value,
                user.Profile.FirstName,
                user.Profile.LastName,
                user.CreatedAt
            );
            
            _logger.LogInformation("User created successfully with ID {UserId}", response.Id);
            
            return Result<CreateUserResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user with email {Email}", command.Email);
            return Result<CreateUserResponse>.Failure(
                Error.Internal("USER_CREATION_FAILED", _userLocalizationService.GetString("UserCreationFailed")));
        }
    }
}