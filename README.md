# Modular Monolith API

> Enterprise-grade user and role management API built with .NET 9, following DDD and CQRS patterns

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-316192?logo=postgresql)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Features](#features)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [API Documentation](#api-documentation)
- [Configuration](#configuration)
- [Deployment](#deployment)
- [Development](#development)
- [License](#license)

## Overview

A production-ready modular monolith implementing enterprise patterns with complete separation of concerns. Features JWT authentication, permission-based authorization, multi-language support, and comprehensive CQRS implementation.

**Key Highlights:**
- 🏗️ **Modular Architecture** - Clean boundaries with DDD and CQRS
- 🎛️ **Feature Management** - Enable/disable modules via configuration at runtime
- 🔐 **Security First** - JWT authentication with refresh tokens and granular permissions
- 🌍 **Multi-Language** - 9 languages with Swagger dropdown selector
- 🚀 **Production Ready** - Docker support, health checks, monitoring, and caching
- 📝 **Type-Safe** - Permission constants system and comprehensive validation
- 📚 **Smart API Docs** - Dynamic Swagger with auto-detected server URLs

## Quick Start

### Using Docker (Recommended)

```bash
# Clone and start
git clone <repository-url>
cd dotnet-base-api
docker-compose up -d

# Access the API
# Swagger UI: http://localhost:8080/swagger
# Health Check: http://localhost:8080/health
```

### Local Development

**Prerequisites:** .NET 9.0 SDK, PostgreSQL 16+, Redis 7+ (optional)

```bash
# Update connection string in src/Api/appsettings.Development.json
cd src/Api
dotnet ef database update
dotnet run

# Access at https://localhost:7000/swagger
```

## Architecture

```
src/
├── Api/                    # Web API with minimal endpoints and middleware
├── Shared/                 # Result pattern, base entities, CQRS interfaces
├── Infrastructure/         # EF Core, repositories, caching, health checks
└── Modules/               # Business modules with domain logic
    ├── Authentication/    # JWT auth with refresh token rotation
    ├── Users/            # User management with CQRS operations
    └── Roles/            # Role and permission management
```

### Design Patterns

- **CQRS** - 3-file pattern: Command/Query, Handler, Validator
- **Result Pattern** - Railway-oriented programming for error handling
- **Repository Pattern** - Generic repositories with caching decorators
- **Value Objects** - Immutable domain concepts (Email, HashedPassword, Permission)
- **Domain Events** - Event-driven architecture for cross-module communication

## Features

### Core Capabilities

| Feature | Description |
|---------|-------------|
| **Authentication** | JWT tokens with refresh rotation, secure logout |
| **Authorization** | Resource-action-scope permission model with type-safe constants |
| **User Management** | CRUD operations with profile management and role assignment |
| **Role Management** | Dynamic roles with granular permission assignments |
| **Module Management** | Enable/disable modules via feature flags at runtime |
| **Localization** | 9 languages with modular JSON resources |
| **Caching** | Redis distributed cache with in-memory fallback |
| **Validation** | FluentValidation with localized error messages |
| **Logging** | Structured logging with Serilog and correlation IDs |
| **Health Checks** | Database, cache, and API monitoring |
| **API Docs** | Complete OpenAPI/Swagger with dynamic server detection |

### Supported Languages

English (en-US) | Spanish (es-ES) | French (fr-FR) | German (de-DE) | Portuguese (pt-BR) | Italian (it-IT) | Japanese (ja-JP) | Chinese (zh-CN) | Indonesian (id-ID)

**Language Detection:** Accept-Language header → Query parameter (`?culture=es-ES`) → Cookie → Fallback to English

## Technology Stack

| Category | Technologies |
|----------|-------------|
| **Framework** | .NET 9.0, ASP.NET Core, C# 12 |
| **Database** | PostgreSQL 16, Entity Framework Core 9.0 |
| **Caching** | Redis 7 (distributed), Memory Cache (local) |
| **Security** | JWT Bearer, BCrypt.Net, FluentValidation |
| **Logging** | Serilog with structured logging |
| **Documentation** | Swashbuckle (OpenAPI 3.0) |
| **DevOps** | Docker, Docker Compose, GitHub Actions |

## Project Structure

### Module Organization

Each module follows consistent CQRS structure:

```
Modules/ModuleName/
├── Domain/
│   ├── Entities/           # Rich domain models with business logic
│   ├── ValueObjects/       # Immutable value types
│   └── Services/           # Domain services
├── Commands/               # Write operations
│   └── OperationName/
│       ├── Command.cs
│       ├── Handler.cs
│       └── Validator.cs
├── Queries/                # Read operations
│   └── QueryName/
│       ├── Query.cs
│       ├── Handler.cs
│       └── Validator.cs
├── Resources/              # Localization JSON files
│   ├── module-messages.json
│   ├── module-messages.es.json
│   └── ...
├── Services/               # Module localization service
├── Endpoints/              # Minimal API endpoints
├── Infrastructure/         # EF Core configurations
└── Authorization/          # Permission constants
```

### Key Components

#### Shared Kernel (`src/Shared/`)

- **BaseEntity** - Guid.CreateVersion7() IDs with audit fields and soft delete
- **Result\<T>** - Error handling with Error types (Validation, NotFound, Conflict)
- **CQRS Interfaces** - ICommand, IQuery, ICommandHandler, IQueryHandler
- **Permission System** - Type-safe constants and centralized registry

#### Infrastructure (`src/Infrastructure/`)

- **Dynamic DbContext** - Automatic entity registration and configuration discovery
- **Repository Pattern** - Generic repositories with caching support
- **Health Checks** - Comprehensive monitoring of all dependencies

## API Documentation

### Swagger UI Features

The API includes a comprehensive Swagger UI with enhanced features:

**🌍 Language Selection Dropdown**
- Accept-Language header with dropdown selector
- Supported languages: en-US, es-ES, fr-FR, de-DE, id-ID
- Automatic localization of error messages and responses

**🔧 Dynamic Server URLs**
- Automatically detects and displays the current server URL
- Works with localhost, custom domains, and reverse proxies
- No manual configuration needed per environment

**🔒 JWT Authentication**
- Built-in "Authorize" button for token management
- Persistent authorization across page refreshes
- Automatic Bearer token header injection

**Access Swagger UI:**
```bash
# Local Development
https://localhost:7000/api-docs

# Docker
http://localhost:8080/api-docs

# Production
https://your-domain.com/api-docs
```

### Authentication Endpoints

```
POST   /api/auth/login          # User login with JWT tokens
POST   /api/auth/refresh        # Refresh access token
POST   /api/auth/logout         # Revoke refresh token
GET    /api/auth/me             # Get current user info
```

### User Management

```
POST   /api/users               # Create user
GET    /api/users/{id}          # Get user by ID
```

### Role Management

```
POST   /api/roles               # Create role with permissions
GET    /api/roles               # List roles (with filters)
GET    /api/roles/{id}          # Get role by ID
PUT    /api/roles/{id}          # Update role
POST   /api/roles/{roleId}/assign/{userId}   # Assign role to user
GET    /api/roles/users/{userId}             # Get user's roles
```

### Permission Discovery

```
GET    /api/permissions                      # Get all permissions
GET    /api/permissions/modules              # Group by module
GET    /api/permissions/resources            # Group by resource
GET    /api/permissions/statistics           # Permission analytics
GET    /api/permissions/search               # Search permissions
GET    /api/permissions/{resource}/{action}  # Find specific permission
```

### System Endpoints

```
GET    /health                  # Health check status
GET    /health/ui               # Health check dashboard
GET    /metrics                 # Application metrics
```

### Example Requests

**Login:**
```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin123!"}'
```

**Get User (with language preference):**
```bash
curl http://localhost:8080/api/users/123 \
  -H "Authorization: Bearer <token>" \
  -H "Accept-Language: es-ES"
```

> **Tip:** In Swagger UI, use the Accept-Language dropdown to easily select your preferred language without manually typing headers.

## Configuration

### Environment Variables

```bash
# Database
ConnectionStrings__DefaultConnection="Host=localhost;Database=ModularMonolith;Username=postgres;Password=postgres123"

# Cache
Cache__Provider="Redis"  # or "InMemory"
Cache__Redis__ConnectionString="localhost:6379"

# JWT
Jwt__Key="your-super-secret-jwt-key-min-32-chars"
Jwt__Issuer="ModularMonolith"
Jwt__Audience="ModularMonolith"
Jwt__AccessTokenExpirationMinutes=15
Jwt__RefreshTokenExpirationDays=7

# Logging
Serilog__MinimumLevel="Information"

# Module Feature Flags
FeatureManagement__Modules__Users="true"
FeatureManagement__Modules__Roles="true"
FeatureManagement__Modules__Authentication="true"
```

### Central Package Management

Uses `Directory.Packages.props` for centralized NuGet version management across all projects.

### Module Feature Management

Control which modules are active without code changes using feature flags:

```json
{
  "FeatureManagement": {
    "Modules": {
      "Users": true,
      "Roles": true,
      "Authentication": true
    }
  }
}
```

**Benefits:**
- **Runtime Control** - Enable/disable modules per environment without redeployment
- **Clean Swagger** - Disabled modules don't appear in API documentation
- **No Side Effects** - Services and endpoints are not registered for disabled modules
- **Environment-Specific** - Different configurations per environment (Development, Staging, Production)

**Usage Examples:**

```bash
# Development - All modules enabled
# appsettings.Development.json
"Modules": {
  "Users": true,
  "Roles": true,
  "Authentication": true
}

# Production - Disable user registration
# appsettings.Production.json
"Modules": {
  "Users": false,  # No user endpoints in Swagger or API
  "Roles": true,
  "Authentication": true
}
```

**Implementation:**

Modules are registered conditionally in `Program.cs`:
```csharp
builder.Services.AddModuleWithFeature<UsersModule>(builder.Configuration, "Users");
builder.Services.AddModuleWithFeature<RolesModule>(builder.Configuration, "Roles");

// Endpoints are also conditional
app.MapModuleEndpointsWithFeature(new Dictionary<Type, string>
{
    { typeof(UsersModule), "Users" },
    { typeof(RolesModule), "Roles" }
});
```

## Deployment

### Docker Compose

```bash
# Development
docker-compose up -d

# Production
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# With monitoring (Seq)
docker-compose --profile monitoring up -d
```

### Included Services

- **API** - .NET 9 application on port 8080
- **PostgreSQL** - Database on port 5432
- **Redis** - Cache on port 6379
- **Seq** - Log analysis on port 5341 (optional)
- **Nginx** - Reverse proxy (production only)

### Health Monitoring

```bash
# Check overall health
curl http://localhost:8080/health

# Response
{
  "status": "Healthy",
  "results": {
    "database": "Healthy",
    "redis": "Healthy",
    "api": "Healthy"
  }
}
```

## Development

### Code Standards

**C# 12 Features:**
- Primary constructors
- Required properties
- Collection expressions
- Pattern matching

**Best Practices:**
- Use permission constants (no hardcoded strings)
- Use module-specific localization services
- Follow 3-file CQRS pattern
- All I/O operations are async
- Internal and sealed by default

### Permission System

**Type-Safe Constants:**
```csharp
// Use permission constants
.RequirePermission(UserPermissions.RESOURCE, UserPermissions.Actions.WRITE)

// Predefined permission objects
.RequireUserPermission(UserPermissions.WriteAll)
.RequireRolePermission(RolePermissions.AssignDepartment)

// Extension methods
.RequireUserWriteConstant()
```

**Create Roles with Permissions:**
```csharp
// Admin role
var adminPermissions = new List<Permission>
{
    UserPermissions.ManageAll,
    RolePermissions.ManageAll,
    AuthenticationPermissions.AuthAdmin
};

// Manager role (department-level)
var managerPermissions = UserPermissions.GetManagerPermissions()
    .Concat(RolePermissions.GetManagerPermissions())
    .ToList();
```

### Localization

**Module-Specific Services:**
```csharp
public class CreateUserHandler
{
    private readonly IUserLocalizationService _localization;
    
    public async Task<Result<CreateUserResponse>> Handle(CreateUserCommand command)
    {
        if (existingUser != null)
        {
            return Result<CreateUserResponse>.Failure(
                Error.Conflict("USER_ALREADY_EXISTS", 
                    _localization.GetString("UserAlreadyExists")));
        }
    }
}
```

**Testing:**
```bash
# Test different languages
curl -H "Accept-Language: id-ID" http://localhost:8080/api/users/invalid-id
curl -H "Accept-Language: es-ES" http://localhost:8080/api/roles
```

### Common Commands

```bash
# Run API locally
dotnet run --project src/Api

# Run tests
dotnet test

# Update database
dotnet ef database update --project src/Api

# Add migration
dotnet ef migrations add MigrationName \
  --project src/Infrastructure \
  --startup-project src/Api

# Start Docker environment
docker-compose up -d

# View logs
docker-compose logs -f api
```

### Testing Strategy

- **Unit Tests** - Domain logic, handlers, validators
- **Integration Tests** - API endpoints, database operations
- **Security Tests** - Authentication and authorization flows

### Adding New Features

**1. Create Module Permission Class:**
```csharp
public static class FeaturePermissions
{
    public const string RESOURCE = "feature";
    
    public static class Actions
    {
        public const string READ = "read";
        public const string WRITE = "write";
    }
    
    public static readonly Permission ReadAll = 
        Permission.Create(RESOURCE, Actions.READ, "*");
}
```

**2. Add Localization Resources:**
```json
// Resources/feature-messages.json
{
  "FeatureNotFound": "Feature not found",
  "FeatureCreated": "Feature created successfully"
}
```

**3. Implement CQRS Operations:**
```
Commands/CreateFeature/
├── CreateFeatureCommand.cs
├── CreateFeatureHandler.cs
└── CreateFeatureValidator.cs
```

**4. Register Module:**
```csharp
// FeatureModule.cs
public static IServiceCollection AddFeatureModule(this IServiceCollection services)
{
    services.AddMediatR(typeof(FeatureModule).Assembly);
    services.AddFeatureLocalization();
    return services;
}
```

## Security

### Authentication & Authorization
- JWT bearer tokens with configurable expiration
- Refresh token rotation with automatic revocation
- Permission-based authorization (resource-action-scope)
- Type-safe permission constants with compile-time validation
- BCrypt password hashing with salt

### API Security
- CORS configuration with whitelisted origins
- Security headers (HSTS, CSP, X-Frame-Options)
- Input validation with FluentValidation
- SQL injection protection via parameterized queries
- XSS protection with content security policy

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Follow the code standards and CQRS patterns
4. Use permission constants and localization services
5. Add tests for new functionality
6. Commit with descriptive messages
7. Push and create a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- 📝 **Documentation** - Check `/swagger` for API docs
- 🐛 **Issues** - Report bugs via GitHub Issues
- 💬 **Discussions** - Ask questions in GitHub Discussions
- 📧 **Contact** - Reach out for enterprise support

---

**Built with .NET 9 | PostgreSQL | Redis | Docker**
