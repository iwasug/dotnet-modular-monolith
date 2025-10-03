# Authorization System

This document describes the permission-based authorization system implemented in the Modular Monolith API.

## Overview

The authorization system provides:
- **JWT Authentication**: Token-based authentication with refresh token rotation
- **Permission-Based Authorization**: Fine-grained access control using Resource-Action-Scope model
- **Role-Based Authorization**: Traditional role-based access control
- **Dynamic Policy Provider**: Automatic policy creation for permissions and roles
- **User Context Service**: Easy access to current user information

## Architecture

### Components

1. **JWT Authentication Middleware** (`JwtAuthenticationMiddleware`)
   - Validates JWT tokens
   - Extracts user claims and sets up context
   - Adds user information to HTTP context items

2. **Authorization Handlers**
   - `PermissionAuthorizationHandler`: Handles permission-based requirements
   - `RoleAuthorizationHandler`: Handles role-based requirements

3. **Authorization Requirements**
   - `PermissionRequirement`: Defines required permissions (Resource:Action:Scope)
   - `RoleRequirement`: Defines required roles (with AND/OR logic)

4. **Dynamic Policy Provider** (`DynamicAuthorizationPolicyProvider`)
   - Creates authorization policies on-demand
   - Supports permission and role policies

5. **User Context Service** (`IUserContext`)
   - Provides easy access to current user information
   - Includes helper methods for role checking

## Permission Model

The system uses a **Resource-Action-Scope** permission model:

- **Resource**: The entity or resource being accessed (e.g., "user", "role", "document")
- **Action**: The operation being performed (e.g., "read", "write", "delete", "*")
- **Scope**: The scope of access (e.g., "self", "team", "organization", "*")

### Permission Examples

```
user:read:*        - Read any user
user:write:self    - Write own user data
role:*:*           - Full role management
*:*:*              - System administrator (all permissions)
```

### Wildcard Support

- `*` in any component matches everything
- Supports hierarchical permission checking
- More specific permissions can override general ones

## Usage

### 1. Minimal API Endpoints

```csharp
using ModularMonolith.Api.Extensions;

// Require specific permission
app.MapGet("/api/users", GetUsers)
   .RequirePermission("user", "read");

// Require permission with scope
app.MapPut("/api/users/{id}", UpdateUser)
   .RequirePermission("user", "write", "self");

// Require any of multiple roles
app.MapGet("/api/admin", GetAdminData)
   .RequireAnyRole("admin", "moderator");

// Require all specified roles
app.MapPost("/api/system/backup", CreateBackup)
   .RequireAllRoles("admin", "backup-operator");

// Using permission shortcuts
app.MapGet("/api/users", GetUsers)
   .RequireUserRead();

app.MapPost("/api/users", CreateUser)
   .RequireUserWrite();
```

### 2. Controller Actions

```csharp
using ModularMonolith.Api.Authorization.Attributes;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    // Require specific permission
    [HttpGet]
    [RequirePermission("user", "read")]
    public IActionResult GetUsers() { ... }

    // Require role
    [HttpDelete("{id}")]
    [RequireRole("admin")]
    public IActionResult DeleteUser(Guid id) { ... }

    // Multiple authorization attributes
    [HttpPost("bulk-import")]
    [RequirePermission("user", "write")]
    [RequireRole("admin", "data-manager")]
    public IActionResult BulkImport() { ... }
}
```

### 3. User Context Service

```csharp
public class SomeService
{
    private readonly IUserContext _userContext;

    public SomeService(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public async Task<Result> DoSomething()
    {
        // Check if user is authenticated
        if (!_userContext.IsAuthenticated)
        {
            return Result.Failure(Error.Unauthorized());
        }

        // Get current user information
        var userId = _userContext.CurrentUserId;
        var email = _userContext.CurrentUserEmail;
        var roleIds = _userContext.CurrentUserRoleIds;

        // Check roles
        if (_userContext.HasAnyRole("admin", "moderator"))
        {
            // User has admin or moderator role
        }

        if (_userContext.HasAllRoles("admin", "backup-operator"))
        {
            // User has both admin and backup-operator roles
        }

        // ... business logic
    }
}
```

## Configuration

### 1. JWT Settings (appsettings.json)

```json
{
  "Jwt": {
    "Key": "your-secret-key-here-must-be-at-least-32-characters-long",
    "Issuer": "ModularMonolith",
    "Audience": "ModularMonolith",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

### 2. Service Registration (Program.cs)

```csharp
// Add HTTP context accessor
builder.Services.AddHttpContextAccessor();

// Add JWT authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add permission-based authorization
builder.Services.AddPermissionBasedAuthorization();

// Add user context service
builder.Services.AddScoped<IUserContext, UserContext>();

// Configure middleware pipeline
app.UseJwtAuthentication();  // Custom JWT middleware
app.UseAuthentication();     // Built-in authentication
app.UseAuthorization();      // Built-in authorization
```

## Common Permission Patterns

### User Management
- `user:read:*` - Read any user
- `user:read:self` - Read own profile
- `user:write:*` - Modify any user
- `user:write:self` - Modify own profile
- `user:delete:*` - Delete any user
- `user:*:*` - Full user management

### Role Management
- `role:read:*` - View roles
- `role:write:*` - Create/modify roles
- `role:delete:*` - Delete roles
- `role:assign:*` - Assign roles to users
- `role:*:*` - Full role management

### System Administration
- `*:*:*` - System administrator (all permissions)
- `*:read:*` - Read-only system access
- `system:backup:*` - System backup operations
- `system:config:*` - System configuration

## Security Considerations

1. **Token Security**
   - Use HTTPS in production
   - Implement proper token rotation
   - Set appropriate token expiration times

2. **Permission Design**
   - Follow principle of least privilege
   - Use specific permissions rather than wildcards when possible
   - Regularly audit permission assignments

3. **Error Handling**
   - Don't expose sensitive information in error messages
   - Log authorization failures for security monitoring
   - Return consistent error responses

4. **Performance**
   - Cache user permissions when possible
   - Optimize database queries for role/permission lookups
   - Consider using Redis for session storage in production

## Testing

### Unit Tests

```csharp
[Test]
public async Task PermissionHandler_ValidPermission_ShouldSucceed()
{
    // Arrange
    var userContext = Mock.Of<IUserContext>(x => 
        x.IsAuthenticated == true &&
        x.CurrentUserId == UserId.From(Guid.NewGuid()) &&
        x.CurrentUserRoleIds == new List<Guid> { roleId });

    var requirement = new PermissionRequirement("user", "read");
    var handler = new PermissionAuthorizationHandler(userContext, roleRepository, logger);

    // Act & Assert
    // ... test implementation
}
```

### Integration Tests

```csharp
[Test]
public async Task GetUsers_WithValidToken_ShouldReturnUsers()
{
    // Arrange
    var token = await GenerateValidTokenAsync();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await client.GetAsync("/api/users");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

## Troubleshooting

### Common Issues

1. **401 Unauthorized**
   - Check if JWT token is included in Authorization header
   - Verify token format: `Bearer <token>`
   - Ensure token is not expired

2. **403 Forbidden**
   - User is authenticated but lacks required permissions
   - Check user's role assignments
   - Verify permission configuration

3. **Policy Not Found**
   - Ensure `DynamicAuthorizationPolicyProvider` is registered
   - Check policy name format
   - Verify authorization service registration

### Debugging

Enable detailed logging for authorization:

```json
{
  "Logging": {
    "LogLevel": {
      "ModularMonolith.Api.Authorization": "Debug",
      "Microsoft.AspNetCore.Authorization": "Debug"
    }
  }
}
```

This will provide detailed logs about authorization decisions and policy evaluations.