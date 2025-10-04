using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Users.Domain;
using ModularMonolith.Users.Services;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Users.Queries.GetUser;

/// <summary>
/// Handler for GetUserQuery following the 3-file pattern
/// </summary>
public class GetUserHandler : IQueryHandler<GetUserQuery, GetUserResponse>
{
    private readonly ILogger<GetUserHandler> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IUserLocalizationService _userLocalizationService;
    
    public GetUserHandler(
        ILogger<GetUserHandler> logger, 
        IUserRepository userRepository,
        IUserLocalizationService userLocalizationService)
    {
        _logger = logger;
        _userRepository = userRepository;
        _userLocalizationService = userLocalizationService;
    }
    
    public async Task<Result<GetUserResponse>> Handle(
        GetUserQuery query, 
        CancellationToken cancellationToken = default)
    {
        using IDisposable activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Query"] = nameof(GetUserQuery),
            ["UserId"] = query.UserId,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        _logger.LogInformation("Getting user with ID {UserId}", query.UserId);
        
        try
        {
            // Get user from repository
            User? user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
            if (user is null)
            {
                _logger.LogWarning("User with ID {UserId} not found", query.UserId);
                return Result<GetUserResponse>.Failure(
                    Error.NotFound("USER_NOT_FOUND", _userLocalizationService.GetString("UserNotFound")));
            }

            // Map to response
            GetUserResponse response = new GetUserResponse(
                user.Id,
                user.Email.Value,
                user.Profile.FirstName,
                user.Profile.LastName,
                user.CreatedAt,
                user.LastLoginAt,
                new List<string>() // TODO: Map from UserRoles when needed
            );
            
            _logger.LogInformation("User retrieved successfully with ID {UserId}", query.UserId);
            
            return Result<GetUserResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with ID {UserId}", query.UserId);
            return Result<GetUserResponse>.Failure(
                Error.NotFound("USER_NOT_FOUND", _userLocalizationService.GetString("UserNotFound")));
        }
    }
}