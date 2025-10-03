# Multi-stage Dockerfile for ModularMonolith API
# Optimized for .NET 9.0 with security best practices

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy solution and project files for better layer caching
COPY ModularMonolith.sln ./
COPY Directory.Packages.props ./
COPY src/Api/ModularMonolith.Api.csproj ./src/Api/
COPY src/Shared/ModularMonolith.Shared.csproj ./src/Shared/
COPY src/Infrastructure/ModularMonolith.Infrastructure.csproj ./src/Infrastructure/
COPY src/Modules/Users/ModularMonolith.Users.csproj ./src/Modules/Users/
COPY src/Modules/Roles/ModularMonolith.Roles.csproj ./src/Modules/Roles/
COPY src/Modules/Authentication/ModularMonolith.Authentication.csproj ./src/Modules/Authentication/

# Restore dependencies
RUN dotnet restore ModularMonolith.sln

# Copy source code
COPY src/ ./src/

# Build the application
WORKDIR /src/src/Api
RUN dotnet build ModularMonolith.Api.csproj -c Release --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish ModularMonolith.Api.csproj -c Release --no-build -o /app/publish \
    --self-contained false \
    --verbosity minimal

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

# Security: Create non-root user
RUN addgroup -g 1001 -S appgroup && \
    adduser -u 1001 -S appuser -G appgroup

# Install security updates and required packages
RUN apk update && \
    apk upgrade && \
    apk add --no-cache \
        ca-certificates \
        tzdata && \
    rm -rf /var/cache/apk/*

# Set working directory
WORKDIR /app

# Copy published application
COPY --from=publish --chown=appuser:appgroup /app/publish .

# Security: Set proper file permissions
RUN chmod -R 755 /app && \
    chmod -R 644 /app/*.dll /app/*.json /app/*.xml || true

# Create directories for logs and temp files with proper permissions
RUN mkdir -p /app/logs /app/temp && \
    chown -R appuser:appgroup /app/logs /app/temp

# Switch to non-root user
USER appuser

# Configure environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

# Entry point
ENTRYPOINT ["dotnet", "ModularMonolith.Api.dll"]