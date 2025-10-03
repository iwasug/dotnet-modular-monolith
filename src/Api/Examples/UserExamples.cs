using ModularMonolith.Users.Commands.CreateUser;
using ModularMonolith.Users.Queries.GetUser;
using Swashbuckle.AspNetCore.Filters;

namespace ModularMonolith.Api.Examples;

/// <summary>
/// Example provider for create user command
/// </summary>
public sealed class CreateUserCommandExample : IExamplesProvider<CreateUserCommand>
{
    public CreateUserCommand GetExamples()
    {
        return new CreateUserCommand(
            Email: "john.doe@example.com",
            Password: "SecurePassword123!",
            FirstName: "John",
            LastName: "Doe"
        );
    }
}

/// <summary>
/// Example provider for create user response
/// </summary>
public sealed class CreateUserResponseExample : IExamplesProvider<CreateUserResponse>
{
    public CreateUserResponse GetExamples()
    {
        return new CreateUserResponse(
            Id: Guid.Parse("12345678-1234-1234-1234-123456789012"),
            Email: "john.doe@example.com",
            FirstName: "John",
            LastName: "Doe",
            CreatedAt: DateTime.UtcNow
        );
    }
}

/// <summary>
/// Example provider for get user response
/// </summary>
public sealed class GetUserResponseExample : IExamplesProvider<GetUserResponse>
{
    public GetUserResponse GetExamples()
    {
        return new GetUserResponse(
            Id: Guid.Parse("12345678-1234-1234-1234-123456789012"),
            Email: "john.doe@example.com",
            FirstName: "John",
            LastName: "Doe",
            CreatedAt: DateTime.UtcNow.AddDays(-30),
            LastLoginAt: DateTime.UtcNow.AddHours(-2),
            Roles: new List<string> { "User", "Manager" }
        );
    }
}