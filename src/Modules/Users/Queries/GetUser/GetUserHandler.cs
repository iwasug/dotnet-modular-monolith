using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Users.Domain;
using ModularMonolith.Users.Domain.ValueObjects;
using ModularMonolith.Users.Services;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Users.Queries.GetUser;

/// <summary>
/// Handler for GetUserQuery following the 3-file pattern
/// </summary>
internal sealed class GetUserHandler(
    ILogger<GetUserHandler> logger,
    IUserRepository userRepository,
    IUserLocalizationService userLocalizationService)
    : IQueryHandler<GetUserQuery, GetUserResponse>
{
    public async Task<Result<GetUserResponse>> Handle(
        GetUserQuery query, 
        CancellationToken cancellationToken = default)
    {
        using IDisposable activity = logger.BeginScope(new Dictionary<string, object>
        {
            ["Query"] = nameof(GetUserQuery),
            ["UserId"] = query.UserId,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        logger.LogInformation("Getting user with ID {UserId}", query.UserId);
        
        try
        {
            // Get user from repository
            UserId userId = UserId.From(query.UserId);
            User? user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                logger.LogWarning("User with ID {UserId} not found", query.UserId);
                return Result<GetUserResponse>.Failure(
                    Error.NotFound("USER_NOT_FOUND", userLocalizationService.GetString("UserNotFound")));
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
            
            logger.LogInformation("User retrieved successfully with ID {UserId}", query.UserId);
            
            return Result<GetUserResponse>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user with ID {UserId}", query.UserId);
            return Result<GetUserResponse>.Failure(
                Error.NotFound("USER_NOT_FOUND", userLocalizationService.GetString("UserNotFound")));
        }
    }
}