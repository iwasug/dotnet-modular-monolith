using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Users.Commands.DeleteUser;

/// <summary>
/// Command to delete a user (soft delete)
/// </summary>
public record DeleteUserCommand(
    Guid Id
) : ICommand<DeleteUserResponse>;

/// <summary>
/// Response for user deletion
/// </summary>
public record DeleteUserResponse(
    Guid Id,
    DateTime DeletedAt
);
