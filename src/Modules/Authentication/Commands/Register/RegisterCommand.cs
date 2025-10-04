using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Authentication.Commands.Register;

/// <summary>
/// Command to register a new user
/// </summary>
public record RegisterCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    string FirstName,
    string LastName
) : ICommand<RegisterResponse>;

/// <summary>
/// Response for user registration
/// </summary>
public record RegisterResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    DateTime CreatedAt,
    string Message
);
