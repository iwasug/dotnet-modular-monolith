# Modular Monolith - Enterprise User Management API

A fully implemented modular monolith for enterprise-grade user and role management using Domain-Driven Design (DDD) and Command Query Responsibility Segregation (CQRS) patterns with .NET 9.

## Table of Contents

- [Architecture](#architecture)
- [Key Features](#key-features)
- [Technology Stack](#technology-stack)
- [Quick Start](#quick-start)
- [Module Structure](#module-structure)
- [Core Components](#core-components)
- [Configuration](#configuration)
- [API Endpoints](#api-endpoints)
- [Multi-Language Support](#multi-language-support)
- [Permission Constants System](#permission-constants-system)
- [Security Features](#security-features)
- [Performance & Monitoring](#performance--monitoring)
- [Docker Deployment](#docker-deployment)
- [Testing Strategy](#testing-strategy)
- [Production Considerations](#production-considerations)
- [Contributing](#contributing)

## Architecture

This project follows a Modular Monolith architecture with complete implementation:

```
src/
├── Api/                              # Web API with minimal APIs, middleware, and security
├── Shared/                           # Shared kernel with Result pattern, base entities, and interfaces
├── Infrastructure/                   # Complete infrastructure with EF Core, caching, and repositories
└── Modules/                          # Fully implemented business modules
    ├── Users/                        # User management with domain entities and CQRS operations
    ├── Roles/                        # Role and permission management with granular access control
    └── Authentication/               # JWT authentication with refresh token rotation
```

## Key Features

- **✅ Modular Architecture**: Complete separation of concerns with well-defined module boundaries
- **✅ CQRS Pattern**: Full command and query separation using 3-file pattern (Command/Query, Handler, Validator)
- **✅ Domain-Driven Design**: Rich domain models with business logic and value objects
- **✅ Result Pattern**: Comprehensive error handling with Result<T> and Error types
- **✅ JWT Authentication**: Secure token-based authentication with refresh token rotation
- **✅ Permission-Based Authorization**: Granular permission system with resource-action-scope model and type-safe constants
- **✅ Entity Framework Core**: PostgreSQL database with dynamic entity registration and configurations
- **✅ Caching**: Redis and in-memory caching with performance optimization
- **✅ Validation**: FluentValidation for all commands and queries
- **✅ Logging**: Structured logging with Serilog and correlation IDs
- **✅ Health Checks**: Comprehensive health monitoring for database, cache, and API
- **✅ API Documentation**: Complete Swagger/OpenAPI documentation with examples
- **✅ Docker Support**: Full containerization with PostgreSQL, Redis, and monitoring
- **✅ Security**: Security headers, CORS, HTTPS enforcement, and JWT middleware
- **✅ Multi-Language Support**: Comprehensive localization with JSON-based resources and modular structure (9 languages supported)
- **✅ Permission Constants**: Type-safe permission system with centralized registry and runtime discovery

## Technology Stack

- **.NET 9.0** - Latest .NET framework with C# 12 features
- **ASP.NET Core** - Web API with minimal APIs and middleware pipeline
- **Entity Framework Core 9.0** - PostgreSQL database with dynamic entity registration
- **PostgreSQL 16** - Primary database with optimized configurations
- **Redis 7** - Distributed caching and session storage
- **JWT Bearer Authentication** - Secure token-based authentication
- **FluentValidation** - Input validation and business rules
- **Serilog** - Structured logging with multiple sinks
- **Swashbuckle** - OpenAPI/Swagger documentation
- **BCrypt.Net** - Password hashing and verification
- **Docker & Docker Compose** - Containerization and orchestration
- **JSON Localization** - Multi-language support with JSON resource files

## Quick Start

### Using Docker (Recommended)

1. **Clone and start all services**
   ```bash
   git clone <repository-url>
   cd ModularMonolith
   docker-compose up -d
   ```

2. **Access the API**
   - API: http://localhost:8080
   - Swagger UI: http://localhost:8080/swagger
   - Health Checks: http://localhost:8080/health

### Local Development

1. **Prerequisites**
   - .NET 9.0 SDK
   - PostgreSQL 16+
   - Redis 7+ (optional)

2. **Setup database**
   ```bash
   # Update connection string in appsettings.Development.json
   cd src/Api
   dotnet ef database update
   ```

3. **Run the API**
   ```bash
   dotnet run --project src/Api
   ```

4. **Access Swagger UI**
   Navigate to `https://localhost:7000/swagger`

## Module Structure

Each module follows a complete CQRS implementation with the 3-file pattern and modular localization:

```
src/Modules/ModuleName/
├── Domain/                    # ✅ Rich domain entities and value objects
│   ├── Entities/             # User, Role, RefreshToken with business logic
│   ├── ValueObjects/         # Email, HashedPassword, RoleName, Permission
│   └── Services/             # Domain services and interfaces
├── Commands/                 # ✅ Write operations (Create, Update, Delete)
│   └── OperationName/        # Command, Handler, Validator (3-file pattern)
├── Queries/                  # ✅ Read operations with filtering and pagination
│   └── QueryName/            # Query, Handler, Validator (3-file pattern)
├── Resources/                # ✅ JSON localization files per language
│   ├── module-messages.json  # Default (English) messages
│   ├── module-messages.id.json # Indonesian messages
│   └── module-messages.es.json # Spanish messages
├── Services/                 # ✅ Module-specific localization services
│   ├── IModuleLocalizationService.cs
│   └── ModuleLocalizationService.cs
├── Endpoints/                # ✅ Minimal API endpoints with authorization
├── Infrastructure/           # ✅ EF Core configurations and repositories
└── ModuleName.csproj         # Module project file
```

### Implemented Modules

#### 🔐 Authentication Module
- **JWT Authentication**: Access and refresh token management
- **Login/Logout**: Secure authentication flow with token rotation
- **Token Refresh**: Automatic token renewal with security validation
- **Password Security**: BCrypt hashing with configurable work factors
- **Localized Messages**: Authentication error messages in multiple languages

#### 👥 Users Module  
- **User Management**: Create, retrieve, and update user profiles
- **Profile Management**: First name, last name, email management
- **Role Assignment**: Link users to roles with audit tracking
- **Password Management**: Secure password changes and validation
- **Localized Messages**: Error messages and validation in multiple languages

#### 🛡️ Roles Module
- **Role Management**: Create, update, and retrieve roles
- **Permission System**: Resource-action-scope based permissions
- **Role Assignment**: Assign/remove roles from users
- **Permission Queries**: Filter roles by permissions and capabilities
- **Localized Messages**: Role-specific error messages in multiple languages

## Core Components

### 🏗️ Shared Kernel (`src/Shared/`)
- **BaseEntity**: Guid.CreateVersion7() IDs with audit fields and soft delete
- **ValueObject**: Immutable value objects with equality comparison
- **Result<T>**: Railway-oriented programming for error handling
- **Error**: Comprehensive error types (Validation, NotFound, Conflict, etc.)
- **IRepository<T>**: Generic repository with caching support
- **CQRS Interfaces**: ICommand, IQuery, ICommandHandler, IQueryHandler
- **Permission System**: Type-safe permission constants and centralized registry
- **Localization Services**: Modular JSON-based localization with per-module resource management

### 🏢 Infrastructure (`src/Infrastructure/`)
- **Dynamic DbContext**: Automatic entity registration and configuration discovery
- **Repository Pattern**: Generic repositories with caching decorators
- **Cache Services**: Redis and in-memory caching with performance monitoring
- **Health Checks**: Database, cache, and application health monitoring
- **Migration Support**: Automated database migrations and validation

### 🌐 API Layer (`src/Api/`)
- **Minimal APIs**: Clean endpoint definitions with automatic validation
- **Security Middleware**: JWT authentication, security headers, CORS
- **Global Exception Handling**: Consistent error responses and logging
- **Swagger Documentation**: Complete API documentation with examples and multi-language support
- **Metrics and Monitoring**: Performance tracking and correlation IDs

## Configuration

### Environment Variables
```bash
# Database
ConnectionStrings__DefaultConnection="Host=localhost;Database=ModularMonolith;Username=postgres;Password=postgres123"

# Cache
Cache__Provider="Redis"  # or "InMemory"
Cache__Redis__ConnectionString="localhost:6379"

# JWT
Jwt__Key="your-super-secret-jwt-key-that-is-at-least-32-characters-long"
Jwt__Issuer="ModularMonolith"
Jwt__Audience="ModularMonolith"
Jwt__AccessTokenExpirationMinutes=15
Jwt__RefreshTokenExpirationDays=7

# Logging
Serilog__MinimumLevel="Information"
Serilog__Seq__ServerUrl="http://localhost:5341"
```

### Package Management
Uses **Central Package Management (CPM)** with `Directory.Packages.props`:
- Centralized version management for all NuGet packages
- Consistent versions across all projects
- Simplified security updates and dependency management

## API Endpoints

### 🔐 Authentication (`/api/auth`)
- `POST /login` - User authentication with JWT tokens
- `POST /refresh` - Refresh access tokens
- `POST /logout` - Revoke refresh tokens
- `GET /me` - Get current user information

### 👥 Users (`/api/users`)
- `POST /` - Create new user
- `GET /{id}` - Get user by ID

### 🛡️ Roles (`/api/roles`)
- `POST /` - Create new role with permissions
- `GET /` - Get roles with filtering and pagination
- `GET /{id}` - Get role by ID with permissions
- `PUT /{id}` - Update role and permissions
- `POST /{roleId}/assign/{userId}` - Assign role to user
- `GET /users/{userId}` - Get user's roles

### 🔑 Permissions (`/api/permissions`)
- `GET /` - Get all registered permissions
- `GET /modules` - Get permissions grouped by module
- `GET /resources` - Get permissions grouped by resource
- `GET /statistics` - Get permission statistics and analytics
- `GET /search` - Search permissions by resource, action, or scope
- `GET /{resource}/{action}` - Find specific permission

### 📊 Monitoring (`/health`, `/metrics`)
- `GET /health` - Application health status
- `GET /health/ui` - Health check dashboard
- `GET /metrics` - Application metrics and performance data

### 🌍 Localization (`/api/localization`)
- `GET /cultures` - Get all supported cultures
- `GET /current-culture` - Get current request culture
- `GET /test-message/{key}` - Test localized message retrieval
- `POST /test-validation` - Test localized validation messages

### 🔧 Modular Localization Testing (`/api/modular-localization-test`)
- `GET /module/{moduleName}/messages` - Test module-specific localized messages
- `GET /all-modules` - Test localization for all modules
- `GET /module-structure` - Get modular localization structure
- `GET /module/{moduleName}/culture/{culture}` - Test module with specific culture

## Multi-Language Support

### 🌍 Comprehensive Localization System

The application implements a comprehensive multi-language support system with modular JSON-based resources, providing localized error messages, validation messages, and API documentation.

#### **Supported Languages**
- **English (en-US)** - Default language
- **Spanish (es-ES)** - Español
- **French (fr-FR)** - Français
- **German (de-DE)** - Deutsch
- **Portuguese (pt-BR)** - Português (Brasil)
- **Italian (it-IT)** - Italiano
- **Japanese (ja-JP)** - 日本語
- **Chinese (zh-CN)** - 中文 (简体)
- **Indonesian (id-ID)** - Bahasa Indonesia

#### **Modular Resource Structure**
```
src/Modules/
├── Users/Resources/
│   ├── user-messages.json      # English user messages
│   ├── user-messages.id.json   # Indonesian user messages
│   └── user-messages.es.json   # Spanish user messages
├── Roles/Resources/
│   ├── role-messages.json      # English role messages
│   ├── role-messages.id.json   # Indonesian role messages
│   └── role-messages.es.json   # Spanish role messages
└── Authentication/Resources/
    ├── auth-messages.json      # English auth messages
    ├── auth-messages.id.json   # Indonesian auth messages
    └── auth-messages.es.json   # Spanish auth messages
```

#### **Language Detection**
The API automatically detects user's preferred language using:
1. **Accept-Language Header** (Primary) - `Accept-Language: id-ID`
2. **Query Parameter** - `?culture=es-ES`
3. **Cookie** - `UserCulture=fr-FR`
4. **Fallback** - Default to English if none match

#### **Localized Error Responses**

**English Response:**
```json
{
  "error": {
    "code": "USER_NOT_FOUND",
    "message": "User not found",
    "type": "NotFound"
  }
}
```

**Indonesian Response:**
```json
{
  "error": {
    "code": "USER_NOT_FOUND",
    "message": "Pengguna tidak ditemukan",
    "type": "NotFound"
  }
}
```

**Spanish Response:**
```json
{
  "error": {
    "code": "USER_NOT_FOUND",
    "message": "Usuario no encontrado",
    "type": "NotFound"
  }
}
```

#### **Module-Specific Localization Services**
Each module has its own localization service for better maintainability:

```csharp
// Users Module
public class CreateUserHandler
{
    private readonly IUserLocalizationService _userLocalizationService;
    
    public async Task<Result<CreateUserResponse>> Handle(CreateUserCommand command)
    {
        if (existingUser != null)
        {
            return Result<CreateUserResponse>.Failure(
                Error.Conflict("USER_ALREADY_EXISTS", 
                    _userLocalizationService.GetString("UserAlreadyExists")));
        }
    }
}

// Roles Module
public class CreateRoleHandler
{
    private readonly IRoleLocalizationService _roleLocalizationService;
    
    public async Task<Result<CreateRoleResponse>> Handle(CreateRoleCommand command)
    {
        if (existingRole != null)
        {
            return Result<CreateRoleResponse>.Failure(
                Error.Conflict("ROLE_ALREADY_EXISTS", 
                    _roleLocalizationService.GetString("RoleAlreadyExists")));
        }
    }
}
```

#### **Testing Localization**
```bash
# Test Indonesian localization
curl -H "Accept-Language: id-ID" http://localhost:8080/api/users/invalid-id

# Test Spanish localization
curl -H "Accept-Language: es-ES" http://localhost:8080/api/roles

# Test module-specific localization
curl http://localhost:8080/api/modular-localization-test/module/Users/culture/id-ID
```

## Permission Constants System

### 🔐 Type-Safe Permission Management

The application implements a comprehensive permission constants system that provides type-safety, discoverability, and centralized management of all permissions across modules.

#### **Architecture**
```
src/Shared/Authorization/
├── PermissionConstants.cs        # Base constants (actions, scopes)
├── IModulePermissions.cs         # Module permission interface
├── PermissionRegistry.cs         # Central permission registry
├── PermissionHelper.cs           # Utility methods
└── README.md                     # Detailed documentation

src/Modules/*/Authorization/
├── *Permissions.cs               # Module-specific permission constants
└── *ModulePermissions.cs         # Module permission implementation
```

#### **Key Features**
- **Type Safety**: No more hardcoded strings, compile-time validation
- **Centralized Registry**: All permissions discoverable via `PermissionRegistry`
- **Module Organization**: Each module defines its own permission constants
- **API Discovery**: REST endpoints to explore all available permissions
- **Predefined Sets**: Common permission groups (Admin, Manager, Basic User)
- **Flexible Scoping**: Support for `*`, `self`, `department`, `organization` scopes

#### **Usage Examples**

**Using Permission Constants:**
```csharp
// Type-safe permission constants
.RequirePermission(UserPermissions.RESOURCE, UserPermissions.Actions.WRITE)

// Predefined permission objects
.RequireUserPermission(UserPermissions.WriteAll)
.RequireRolePermission(RolePermissions.AssignDepartment)

// Extension methods with constants
.RequireUserWriteConstant()
.RequireRoleAssignConstant()
```

**Creating Roles with Permissions:**
```csharp
// Admin role with full permissions
var adminPermissions = new List<Permission>
{
    UserPermissions.ManageAll,
    RolePermissions.ManageAll,
    AuthenticationPermissions.AuthAdmin
};

// Manager role with department-level permissions
var managerPermissions = UserPermissions.GetManagerPermissions()
    .Concat(RolePermissions.GetManagerPermissions())
    .ToList();

// Basic user with self-permissions only
var basicPermissions = UserPermissions.GetBasicPermissions();
```

**Permission Discovery:**
```csharp
// Inject PermissionRegistry
public class RoleService
{
    private readonly PermissionRegistry _permissionRegistry;
    
    public async Task<List<Permission>> GetAvailablePermissions()
    {
        // Get all permissions across all modules
        return _permissionRegistry.GetAllPermissions().ToList();
    }
    
    public async Task<List<Permission>> GetUserModulePermissions()
    {
        // Get permissions for specific module
        return _permissionRegistry.GetModulePermissions("Users").ToList();
    }
}
```

#### **Available Permission Sets**

**User Permissions:**
- `UserPermissions.ReadAll/ReadSelf/ReadDepartment`
- `UserPermissions.WriteAll/WriteSelf/WriteDepartment`
- `UserPermissions.CreateAll/CreateDepartment`
- `UserPermissions.UpdateAll/UpdateSelf/UpdateDepartment`
- `UserPermissions.DeleteAll/DeleteDepartment`
- `UserPermissions.ManageAll/ManageDepartment`

**Role Permissions:**
- `RolePermissions.ReadAll/ReadDepartment/ReadOrganization`
- `RolePermissions.WriteAll/WriteDepartment`
- `RolePermissions.CreateAll/CreateDepartment`
- `RolePermissions.UpdateAll/UpdateDepartment`
- `RolePermissions.DeleteAll/DeleteDepartment`
- `RolePermissions.AssignAll/AssignDepartment`
- `RolePermissions.RevokeAll/RevokeDepartment`
- `RolePermissions.ManageAll/ManageDepartment`

**Authentication Permissions:**
- `AuthenticationPermissions.LoginAll`
- `AuthenticationPermissions.LogoutAll/LogoutSelf`
- `AuthenticationPermissions.RefreshTokenSelf`
- `AuthenticationPermissions.RevokeTokenAll/RevokeTokenSelf/RevokeTokenDepartment`
- `AuthenticationPermissions.ReadSessionAll/ReadSessionSelf/ReadSessionDepartment`
- `AuthenticationPermissions.ManageSessionAll/ManageSessionDepartment`
- `AuthenticationPermissions.AuthAdmin/TokenAdmin/SessionAdmin`

#### **Permission Discovery API**

The system provides REST endpoints for runtime permission discovery:

```bash
# Get all permissions
GET /api/permissions

# Get permissions by module
GET /api/permissions/modules

# Get permissions by resource
GET /api/permissions/resources

# Get permission statistics
GET /api/permissions/statistics

# Search permissions
GET /api/permissions/search?resource=user&action=read

# Find specific permission
GET /api/permissions/user/read?scope=*
```

#### **Adding New Permissions**

1. **Define in Module Permission Class:**
```csharp
public static class UserPermissions
{
    // Add new action
    public static class Actions
    {
        public const string EXPORT = "export";
    }
    
    // Add new scope
    public static class Scopes
    {
        public const string TEAM = "team";
    }
    
    // Add predefined permission
    public static readonly Permission ExportTeam = 
        Permission.Create(RESOURCE, Actions.EXPORT, Scopes.TEAM);
    
    // Update GetAllPermissions()
    public static IReadOnlyList<Permission> GetAllPermissions()
    {
        return new List<Permission> { /* ... existing ..., */ ExportTeam };
    }
}
```

2. **Use in Endpoints:**
```csharp
.RequirePermission(UserPermissions.RESOURCE, UserPermissions.Actions.EXPORT, UserPermissions.Scopes.TEAM)
```

3. **Automatic Registration:**
The `PermissionRegistry` automatically discovers and registers all module permissions at startup.

## Development Guidelines

### 🎯 Core Principles
- **C# 12 Features**: Primary constructors, required properties, collection expressions
- **SOLID Principles**: Single responsibility, dependency injection, interface segregation
- **Result Pattern**: No exceptions for business logic, use Result<T> for error handling
- **3-File CQRS**: Command/Query, Handler, Validator for each operation
- **Value Objects**: Immutable types for domain concepts (Email, RoleName, etc.)

### 🔐 Security & Permissions
- **Permission Constants**: Always use constants instead of hardcoded permission strings
- **Type-Safe Authorization**: Use predefined permission objects and extension methods
- **Centralized Registry**: Register all permissions in module permission classes

### 🌍 Localization
- **Localized Messages**: Use module-specific localization services for all user-facing messages
- **JSON Resources**: Manage translations in JSON files for better maintainability
- **Modular Structure**: Each module manages its own localization resources

### 💻 Code Quality
- **Async/Await**: All I/O operations are asynchronous
- **Explicit Typing**: Avoid `var` except when type is obvious
- **Internal by Default**: Types are internal and sealed unless public access needed
- **Comprehensive Testing**: Unit tests for business logic, integration tests for APIs
## Security Features

### 🔒 Authentication & Authorization
- **JWT Bearer Tokens**: Secure access tokens with configurable expiration
- **Refresh Token Rotation**: Automatic token renewal with revocation support
- **Permission-Based Authorization**: Fine-grained access control with resource-action-scope model
- **Type-Safe Permissions**: Compile-time validated permission constants with centralized registry
- **Dynamic Permission Discovery**: Runtime exploration of all available permissions
- **Granular Scoping**: Support for `*`, `self`, `department`, `organization` permission scopes
- **Password Security**: BCrypt hashing with salt and configurable work factors
- **Security Headers**: HSTS, CSP, X-Frame-Options, and other security headers

### 🛡️ API Security
- **CORS Configuration**: Secure cross-origin resource sharing policies
- **Rate Limiting**: Protection against abuse and DoS attacks
- **Input Validation**: Comprehensive validation using FluentValidation
- **SQL Injection Protection**: Parameterized queries and EF Core safeguards
- **XSS Protection**: Content Security Policy and input sanitization

## Performance & Monitoring

### 📈 Caching Strategy
- **Multi-Level Caching**: Redis for distributed caching, in-memory for local caching
- **Cache-Aside Pattern**: Efficient cache management with automatic invalidation
- **Performance Monitoring**: Cache hit rates and performance metrics
- **Optimized Queries**: Query optimization and performance analysis

### 📊 Observability
- **Structured Logging**: Serilog with correlation IDs and contextual information
- **Health Checks**: Comprehensive monitoring of database, cache, and external dependencies
- **Metrics Collection**: Application performance metrics and business KPIs
- **Distributed Tracing**: Request correlation across service boundaries

## Docker Deployment

### 🐳 Container Configuration
```bash
# Development environment
docker-compose up -d

# Production environment  
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# With monitoring stack
docker-compose --profile monitoring up -d
```

### 📦 Included Services
- **API Container**: .NET 9 application with optimized runtime
- **PostgreSQL 16**: Primary database with initialization scripts
- **Redis 7**: Distributed cache and session storage
- **Seq**: Centralized logging and log analysis (optional)
- **Nginx**: Reverse proxy and load balancer (production profile)

## Testing Strategy

### 🧪 Test Architecture
- **Unit Tests**: Domain logic and business rules validation
- **Integration Tests**: API endpoints and database operations
- **Repository Tests**: Data access layer validation
- **Authentication Tests**: Security and authorization flows

### 🔍 Test Coverage
- Domain entities and value objects
- CQRS command and query handlers
- Repository implementations
- API endpoint security and validation
- Cache service functionality
- Localization services and message retrieval
- Multi-language error responses and validation messages

## Production Considerations

### 🚀 Deployment
- **Health Checks**: Kubernetes-ready health endpoints
- **Graceful Shutdown**: Proper application lifecycle management
- **Configuration Management**: Environment-based configuration
- **Secret Management**: Secure handling of sensitive configuration
- **Database Migrations**: Automated schema updates and rollback support

### 📊 Monitoring & Alerting
- **Application Metrics**: Performance counters and business metrics
- **Error Tracking**: Comprehensive error logging and alerting
- **Performance Monitoring**: Response times and throughput analysis
- **Resource Utilization**: CPU, memory, and database performance

## Contributing

### 🛠️ Development Setup
1. Install .NET 9.0 SDK
2. Install Docker Desktop
3. Clone repository and run `docker-compose up -d`
4. Run `dotnet ef database update` in `src/Api`
5. Start development with `dotnet run --project src/Api`

### 📋 Code Standards
- Follow the established CQRS patterns
- Use permission constants instead of hardcoded strings
- Use module-specific localization services for all user-facing messages
- Implement comprehensive validation with localized messages
- Add appropriate logging and error handling
- Include unit tests for new functionality
- Update API documentation and examples
- Register new permissions in module permission classes
- Add JSON resource files for new languages when adding localization

### 🔄 CI/CD Pipeline
- Automated testing on pull requests
- Code quality analysis and security scanning
- Automated deployment to staging environments
- Production deployment with approval gates

## Quick Reference

### 🚀 Common Commands
```bash
# Start development environment
docker-compose up -d

# Run API locally
dotnet run --project src/Api

# Run tests
dotnet test

# Update database
dotnet ef database update --project src/Api

# Add migration
dotnet ef migrations add MigrationName --project src/Infrastructure --startup-project src/Api
```

### 🔗 Important URLs
- **API**: http://localhost:8080 (Docker) or https://localhost:7000 (Local)
- **Swagger UI**: `/swagger`
- **Health Checks**: `/health`
- **Metrics**: `/metrics`
- **Permission Discovery**: `/api/permissions`
- **Localization Test**: `/api/modular-localization-test/all-modules`

### 📁 Key Directories
- `src/Modules/*/Commands/` - Write operations (CQRS)
- `src/Modules/*/Queries/` - Read operations (CQRS)
- `src/Modules/*/Resources/` - Localization JSON files
- `src/Modules/*/Authorization/` - Permission constants
- `src/Shared/Authorization/` - Permission system core
- `src/Api/Endpoints/` - API endpoint definitions

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For questions, issues, or contributions:
- Create an issue in the GitHub repository
- Follow the contributing guidelines
- Check the documentation and examples
- Review the test suite for usage patterns

---

**Built with ❤️ using .NET 9, PostgreSQL, Redis, and comprehensive multi-language support**