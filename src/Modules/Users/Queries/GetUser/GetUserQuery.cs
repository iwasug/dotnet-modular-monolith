using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Users.Queries.GetUser;

/// <summary>
/// Query to get a user by ID
/// </summary>
public record GetUserQuery(Guid UserId) : IQuery<GetUserResponse>;

/// <summary>
/// Response for user query
/// </summary>
public record GetUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    List<string> Roles
);