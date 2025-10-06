using ModularMonolith.Api.Extensions;
using ModularMonolith.Api.Services;
using ModularMonolith.Authentication;
using ModularMonolith.Infrastructure;
using ModularMonolith.Roles;
using ModularMonolith.Shared.Extensions;
using ModularMonolith.Users;
using ModularMonolith.Api.Endpoints;
using ModularMonolith.Api.Middleware;
using Serilog;

// Configure Serilog early in the application startup
var builder = WebApplication.CreateBuilder(args);

// Configure Serilog structured logging
builder.ConfigureSerilog();

// Add services to the container
builder.Services.AddControllers();

// Add comprehensive localization support
builder.Services.AddComprehensiveLocalization();

// Add comprehensive Swagger/OpenAPI documentation
builder.Services.AddComprehensiveSwagger(builder.Configuration);

// Add HTTP context accessor for user context
builder.Services.AddHttpContextAccessor();

// Add comprehensive security services
builder.Services.AddComprehensiveSecurity(builder.Configuration);

// Add CORS with secure configuration
builder.Services.AddSecureCors(builder.Configuration);

// Add JWT authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add permission-based authorization
builder.Services.AddPermissionBasedAuthorization();

// Add user context service
builder.Services.AddScoped<IUserContext, UserContext>();

// Add metrics service
builder.Services.AddSingleton<IMetricsService, MetricsService>();

// Add comprehensive health checks
builder.Services.AddComprehensiveHealthChecks(builder.Configuration);

// Register modular architecture
builder.Services.AddSharedKernel(builder.Configuration);
builder.Services.AddModule<UsersModule>();
builder.Services.AddModule<RolesModule>();
builder.Services.AddModule<AuthenticationModule>();
builder.Services.AddModule<InfrastructureModule>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseComprehensiveSwagger(app.Environment);
}

// Use global exception handling (should be early in the pipeline)
app.UseGlobalExceptionHandling();

// Use comprehensive security middleware (includes HTTPS, HSTS, security headers, CORS)
app.UseComprehensiveSecurity();

// Use comprehensive localization middleware
app.UseComprehensiveLocalization();

// Use localized validation middleware
app.UseLocalizedValidation();

// Use correlation ID middleware for request tracking
app.UseMiddleware<CorrelationIdMiddleware>();

// Use metrics middleware for performance tracking
app.UseMiddleware<MetricsMiddleware>();

// Use structured request logging
app.UseStructuredRequestLogging();

// Use JWT authentication middleware (custom middleware for additional context setup)
app.UseJwtAuthentication();

// Use built-in authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map comprehensive health check endpoints
app.MapComprehensiveHealthChecks();

// Map module endpoints automatically
app.MapModuleEndpoints(
    typeof(UsersModule).Assembly,
    typeof(RolesModule).Assembly,
    typeof(AuthenticationModule).Assembly
);

// Map API-specific endpoints
app.MapMetricsEndpoints();

#if DEBUG
// Map debug endpoints (Development only)
new ConfigurationDebugEndpoints().MapEndpoints(app);
new EntityDiscoveryEndpoints().MapEndpoints(app);
#endif

// Map permission endpoints
app.MapPermissionEndpoints();

// Map localization endpoints
new LocalizationEndpoints().MapEndpoints(app);

// Map JSON localization test endpoints
new JsonLocalizationTestEndpoints().MapEndpoints(app);

// Map modular localization test endpoints
new ModularLocalizationTestEndpoints().MapEndpoints(app);

// Map API response example endpoints
new ApiResponseExampleEndpoints().MapEndpoints(app);

app.MapControllers();

try
{
    Log.Information("Starting ModularMonolith API application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}