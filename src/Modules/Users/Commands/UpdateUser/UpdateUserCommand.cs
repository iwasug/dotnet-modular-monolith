using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Users.Commands.UpdateUser;

/// <summary>
/// Command to update an existing user
/// </summary>
public record UpdateUserCommand(
    Guid Id,
    string Email,
    string FirstName,
    string LastName
) : ICommand<UpdateUserResponse>;

/// <summary>
/// Response for user update
/// </summary>
public record UpdateUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    DateTime UpdatedAt
);
