using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using ModularMonolith.Api.Services;
using Swashbuckle.AspNetCore.Filters;

namespace ModularMonolith.Api.Extensions;

/// <summary>
/// Extension methods for configuring Swagger/OpenAPI documentation
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// Adds comprehensive Swagger/OpenAPI configuration with JWT authentication support
    /// </summary>
    public static IServiceCollection AddComprehensiveSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        
        // Register API documentation service for localization
        services.AddSingleton<IApiDocumentationService, ApiDocumentationService>();
        
        services.AddSwaggerGen(options =>
        {
            // Get API documentation service from DI
            var serviceProvider = services.BuildServiceProvider();
            var apiDocService = serviceProvider.GetRequiredService<IApiDocumentationService>();
            
            // Basic API information
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = apiDocService.GetApiTitle(),
                Description = apiDocService.GetApiDescription(),
                Contact = new OpenApiContact
                {
                    Name = "API Support",
                    Email = "support@modularmonolith.com",
                    Url = new Uri("https://github.com/modularmonolith/enterprise-user-management")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                },
                TermsOfService = new Uri("https://modularmonolith.com/terms")
            });

            // JWT Authentication configuration
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = apiDocService.GetAuthenticationDescription()
            });

            // Global security requirement for JWT
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Include XML comments for comprehensive documentation
            var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml");
            foreach (var xmlFile in xmlFiles)
            {
                options.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
            }

            // Custom schema filters for better documentation
            options.SchemaFilter<EnumSchemaFilter>();
            options.SchemaFilter<RequiredNotNullableSchemaFilter>();
            
            // Operation filters for enhanced documentation
            options.OperationFilter<SecurityRequirementsOperationFilter>();
            options.OperationFilter<LocalizationOperationFilter>();
            options.OperationFilter<ResponseHeadersOperationFilter>();
            
            // Document filters for additional customization
            options.DocumentFilter<TagOrderDocumentFilter>();
            options.DocumentFilter<LocalizationDocumentFilter>();

            // Enable annotations for richer documentation
            options.EnableAnnotations();
            
            // Custom schema IDs to avoid conflicts
            options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
            
            // Support for polymorphism
            options.UseAllOfToExtendReferenceSchemas();
            options.UseAllOfForInheritance();
            options.UseOneOfForPolymorphism();
            
            // Configure example providers
            options.ExampleFilters();
        });

        // Add example filters for better API documentation
        services.AddSwaggerExamplesFromAssemblyOf<Program>();
        
        return services;
    }

    /// <summary>
    /// Configures Swagger UI with comprehensive settings and localization support
    /// </summary>
    public static IApplicationBuilder UseComprehensiveSwagger(this IApplicationBuilder app, IWebHostEnvironment environment)
    {
        app.UseSwagger(options =>
        {
            options.RouteTemplate = "api-docs/{documentName}/swagger.json";
            options.PreSerializeFilters.Add((swagger, httpReq) =>
            {
                // Dynamically set server URL based on the actual request
                var scheme = httpReq.Scheme;
                var host = httpReq.Host.Value;
                var serverUrl = $"{scheme}://{host}";
                
                swagger.Servers = new List<OpenApiServer>
                {
                    new OpenApiServer
                    {
                        Url = serverUrl,
                        Description = $"Current Server ({scheme.ToUpper()})"
                    }
                };
                
                // Add localization support based on Accept-Language header
                var acceptLanguage = httpReq.Headers["Accept-Language"].FirstOrDefault();
                if (!string.IsNullOrEmpty(acceptLanguage))
                {
                    var culture = acceptLanguage.Split(',').FirstOrDefault()?.Split(';').FirstOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(culture))
                    {
                        swagger.Info.Description = GetLocalizedApiDescription(culture);
                    }
                }
            });
        });

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/api-docs/v1/swagger.json", "Enterprise User Management API v1");
            options.RoutePrefix = "api-docs";
            
            // Enhanced UI configuration
            options.DocumentTitle = "Enterprise User Management API Documentation";
            options.DefaultModelsExpandDepth(2);
            options.DefaultModelExpandDepth(2);
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
            options.EnableTryItOutByDefault();
            options.ShowExtensions();
            options.ShowCommonExtensions();
            
            // Custom CSS for better appearance
            options.InjectStylesheet("/swagger-ui/custom.css");
            
            // Custom JavaScript for enhanced functionality (Note: InjectJavaScript not available in this version)
            // options.InjectJavaScript("/swagger-ui/custom.js");
            
            // OAuth2 configuration for JWT (if needed in the future)
            options.OAuthClientId("swagger-ui");
            options.OAuthAppName("Enterprise User Management API");
            options.OAuthUsePkce();
            
            // Configure supported submit methods
            options.SupportedSubmitMethods(
                Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Get, 
                Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Post, 
                Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Put, 
                Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Delete);
            
            // Persistence configuration
            options.EnablePersistAuthorization();
            
            // Configure validator URL (disable for security in production)
            if (environment.IsDevelopment())
            {
                options.EnableValidator();
            }
            else
            {
                options.EnableValidator(null);
            }
        });

        return app;
    }

    private static string GetApiDescription()
    {
        return """
            A comprehensive enterprise-grade API for user and role management built with .NET 9.0 and Modular Monolith architecture.
            
            ## Features
            - **User Management**: Complete CRUD operations for user accounts
            - **Role-Based Access Control**: Granular permission system with resource-action-scope model
            - **JWT Authentication**: Secure token-based authentication with refresh token rotation
            - **Multi-Language Support**: Localized responses based on Accept-Language header
            - **Caching**: Redis and In-Memory caching for optimal performance
            - **Monitoring**: Comprehensive health checks and structured logging
            - **Standardized API Responses**: All endpoints return responses in a consistent format
            
            ## Standardized API Response Structure
            All endpoints return responses wrapped in a standardized format:
            
            **Success Response:**
            ```json
            {
              "success": true,
              "data": { /* actual response data */ },
              "message": "Operation completed successfully",
              "timestamp": "2025-10-05T12:34:56Z",
              "error": null
            }
            ```
            
            **Error Response:**
            ```json
            {
              "success": false,
              "data": null,
              "message": "Error description",
              "timestamp": "2025-10-05T12:34:56Z",
              "error": {
                "code": "ERROR_CODE",
                "message": "Detailed error message",
                "type": "NotFound|Validation|Conflict|Unauthorized|Forbidden|Internal"
              }
            }
            ```
            
            ## Authentication
            This API uses JWT Bearer tokens for authentication. To access protected endpoints:
            1. Obtain a token by calling the `/api/auth/login` endpoint
            2. Include the token in the Authorization header: `Bearer <your-token>`
            3. Refresh tokens using the `/api/auth/refresh` endpoint when needed
            
            ## Permissions
            The API implements a granular permission system with the format: `resource:action:scope`
            - **Resources**: user, role, auth
            - **Actions**: read, write, delete, assign
            - **Scopes**: own, team, organization, global
            
            ## Rate Limiting
            API endpoints are rate-limited to ensure fair usage and system stability.
            """;
    }

    private static string GetJwtDescription()
    {
        return """
            JWT Bearer token authentication. 
            
            **Format**: `Bearer <token>`
            
            **How to obtain a token**:
            1. Call the `/api/auth/login` endpoint with valid credentials
            2. Use the returned `accessToken` in the Authorization header
            3. Refresh the token using `/api/auth/refresh` before it expires
            
            **Token Expiration**: Access tokens expire in 15 minutes, refresh tokens in 7 days.
            """;
    }

    private static string GetLocalizedApiDescription(string culture)
    {
        return culture.ToLowerInvariant() switch
        {
            "es" or "es-es" => """
                Una API integral de nivel empresarial para la gestión de usuarios y roles construida con .NET 9.0 y arquitectura Monolito Modular.
                
                ## Características
                - **Gestión de Usuarios**: Operaciones CRUD completas para cuentas de usuario
                - **Control de Acceso Basado en Roles**: Sistema de permisos granular con modelo recurso-acción-alcance
                - **Autenticación JWT**: Autenticación segura basada en tokens con rotación de tokens de actualización
                - **Soporte Multi-idioma**: Respuestas localizadas basadas en el encabezado Accept-Language
                """,
            "fr" or "fr-fr" => """
                Une API complète de niveau entreprise pour la gestion des utilisateurs et des rôles construite avec .NET 9.0 et l'architecture Monolithe Modulaire.
                
                ## Fonctionnalités
                - **Gestion des Utilisateurs**: Opérations CRUD complètes pour les comptes utilisateur
                - **Contrôle d'Accès Basé sur les Rôles**: Système de permissions granulaire avec modèle ressource-action-portée
                - **Authentification JWT**: Authentification sécurisée basée sur des tokens avec rotation des tokens de rafraîchissement
                - **Support Multi-langues**: Réponses localisées basées sur l'en-tête Accept-Language
                """,
            "de" or "de-de" => """
                Eine umfassende Unternehmens-API für Benutzer- und Rollenverwaltung, erstellt mit .NET 9.0 und Modularer Monolith-Architektur.
                
                ## Funktionen
                - **Benutzerverwaltung**: Vollständige CRUD-Operationen für Benutzerkonten
                - **Rollenbasierte Zugriffskontrolle**: Granulares Berechtigungssystem mit Ressource-Aktion-Bereich-Modell
                - **JWT-Authentifizierung**: Sichere token-basierte Authentifizierung mit Refresh-Token-Rotation
                - **Mehrsprachige Unterstützung**: Lokalisierte Antworten basierend auf Accept-Language-Header
                """,
            _ => GetApiDescription()
        };
    }
}

/// <summary>
/// Schema filter to properly document enum values
/// </summary>
public sealed class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            schema.Enum.Clear();
            foreach (var enumValue in Enum.GetValues(context.Type))
            {
                schema.Enum.Add(new Microsoft.OpenApi.Any.OpenApiString(enumValue.ToString()));
            }
            schema.Type = "string";
            schema.Format = null;
        }
    }
}

/// <summary>
/// Schema filter to mark required properties as not nullable
/// </summary>
public sealed class RequiredNotNullableSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties is null)
            return;

        foreach (var property in schema.Properties)
        {
            if (schema.Required?.Contains(property.Key) == true)
            {
                property.Value.Nullable = false;
            }
        }
    }
}

/// <summary>
/// Operation filter to add security requirements based on endpoint attributes
/// </summary>
public sealed class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAllowAnonymous = context.MethodInfo.GetCustomAttributes(true)
            .Union(context.MethodInfo.DeclaringType?.GetCustomAttributes(true) ?? Array.Empty<object>())
            .Any(x => x.GetType().Name == "AllowAnonymousAttribute");

        if (hasAllowAnonymous)
        {
            operation.Security?.Clear();
            return;
        }

        // Add security requirement for protected endpoints
        operation.Security ??= new List<OpenApiSecurityRequirement>();
        
        if (!operation.Security.Any())
        {
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        }
    }
}

/// <summary>
/// Operation filter to add localization information to operations
/// </summary>
public sealed class LocalizationOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Add Accept-Language header parameter for localization
        operation.Parameters ??= new List<OpenApiParameter>();
        
        if (!operation.Parameters.Any(p => p.Name == "Accept-Language"))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Accept-Language",
                In = ParameterLocation.Header,
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Enum = new List<Microsoft.OpenApi.Any.IOpenApiAny>
                    {
                        new Microsoft.OpenApi.Any.OpenApiString("en-US"),
                        new Microsoft.OpenApi.Any.OpenApiString("es-ES"),
                        new Microsoft.OpenApi.Any.OpenApiString("fr-FR"),
                        new Microsoft.OpenApi.Any.OpenApiString("de-DE"),
                        new Microsoft.OpenApi.Any.OpenApiString("id-ID")
                    },
                    Default = new Microsoft.OpenApi.Any.OpenApiString("en-US")
                },
                Description = "Preferred language for localized responses. Supported languages: en-US (English), es-ES (Spanish), fr-FR (French), de-DE (German), id-ID (Indonesian)"
            });
        }
    }
}

/// <summary>
/// Operation filter to document response headers
/// </summary>
public sealed class ResponseHeadersOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Add common response headers
        foreach (var response in operation.Responses.Values)
        {
            response.Headers ??= new Dictionary<string, OpenApiHeader>();
            
            if (!response.Headers.ContainsKey("X-Correlation-ID"))
            {
                response.Headers.Add("X-Correlation-ID", new OpenApiHeader
                {
                    Description = "Unique identifier for request correlation and tracing",
                    Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
                });
            }
            
            if (!response.Headers.ContainsKey("X-RateLimit-Remaining"))
            {
                response.Headers.Add("X-RateLimit-Remaining", new OpenApiHeader
                {
                    Description = "Number of requests remaining in the current rate limit window",
                    Schema = new OpenApiSchema { Type = "integer" }
                });
            }
        }
    }
}

/// <summary>
/// Document filter to order tags logically
/// </summary>
public sealed class TagOrderDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var tagOrder = new[] { "Authentication", "Users", "Roles", "Health", "Metrics" };
        
        swaggerDoc.Tags = swaggerDoc.Tags
            ?.OrderBy(tag => Array.IndexOf(tagOrder, tag.Name))
            .ThenBy(tag => tag.Name)
            .ToList();
    }
}

/// <summary>
/// Document filter to add localization information to the document
/// </summary>
public sealed class LocalizationDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Add extension for supported languages
        swaggerDoc.Extensions.Add("x-supported-languages", new Microsoft.OpenApi.Any.OpenApiArray
        {
            new Microsoft.OpenApi.Any.OpenApiString("en-US"),
            new Microsoft.OpenApi.Any.OpenApiString("es-ES"),
            new Microsoft.OpenApi.Any.OpenApiString("fr-FR"),
            new Microsoft.OpenApi.Any.OpenApiString("de-DE"),
            new Microsoft.OpenApi.Any.OpenApiString("id-ID")
        });
        
        // Add extension for API versioning
        swaggerDoc.Extensions.Add("x-api-version", new Microsoft.OpenApi.Any.OpenApiString("1.0"));
        
        // Add extension for rate limiting information
        swaggerDoc.Extensions.Add("x-rate-limiting", new Microsoft.OpenApi.Any.OpenApiObject
        {
            ["global"] = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["limit"] = new Microsoft.OpenApi.Any.OpenApiInteger(100),
                ["window"] = new Microsoft.OpenApi.Any.OpenApiString("1 minute")
            },
            ["auth"] = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["limit"] = new Microsoft.OpenApi.Any.OpenApiInteger(5),
                ["window"] = new Microsoft.OpenApi.Any.OpenApiString("15 minutes")
            }
        });
    }
}