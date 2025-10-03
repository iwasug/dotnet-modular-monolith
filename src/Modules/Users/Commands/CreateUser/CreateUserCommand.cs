using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Users.Commands.CreateUser;

/// <summary>
/// Command to create a new user
/// </summary>
public record CreateUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName
) : ICommand<CreateUserResponse>;

/// <summary>
/// Response for user creation
/// </summary>
public record CreateUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    DateTime CreatedAt
);