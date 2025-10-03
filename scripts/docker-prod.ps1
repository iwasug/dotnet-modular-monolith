# Production Docker Compose helper script for Windows

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("start", "stop", "restart", "logs", "clean", "migrate", "help")]
    [string]$Command,
    
    [Parameter(Mandatory=$false)]
    [string]$Service
)

# Colors for output
$Red = "Red"
$Green = "Green"
$Yellow = "Yellow"

function Write-Status {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor $Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor $Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor $Red
}

# Check if .env file exists
if (-not (Test-Path ".env")) {
    Write-Warning ".env file not found. Creating from .env.example..."
    Copy-Item ".env.example" ".env"
    Write-Status "Please update .env file with your production configuration"
    Write-Warning "Make sure to set strong passwords and proper SSL certificates!"
    exit 1
}

function Start-Production {
    Write-Status "Starting production environment..."
    
    # Validate required environment variables
    $envContent = Get-Content ".env" | Where-Object { $_ -match "=" }
    $requiredVars = @("POSTGRES_PASSWORD", "REDIS_PASSWORD", "JWT_SECRET_KEY")
    
    foreach ($var in $requiredVars) {
        $found = $envContent | Where-Object { $_ -match "^$var=" }
        if (-not $found -or $found -match "=\s*$") {
            Write-Error "Required environment variable $var is not set in .env file"
            exit 1
        }
    }
    
    docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
    
    Write-Status "Waiting for services to be healthy..."
    Start-Sleep -Seconds 30
    
    Write-Status "Production environment is ready!"
    Write-Status "API: http://localhost:8080"
    Write-Status "Nginx: http://localhost:80 (if enabled)"
    Write-Status "Health Check: http://localhost:8080/health"
}

function Stop-Production {
    Write-Status "Stopping production environment..."
    docker-compose -f docker-compose.yml -f docker-compose.prod.yml down
    Write-Status "Production environment stopped"
}

function Restart-Production {
    Write-Status "Restarting production environment..."
    docker-compose -f docker-compose.yml -f docker-compose.prod.yml down
    docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
    Write-Status "Production environment restarted"
}

function Show-Logs {
    if ($Service) {
        docker-compose -f docker-compose.yml -f docker-compose.prod.yml logs -f $Service
    } else {
        docker-compose -f docker-compose.yml -f docker-compose.prod.yml logs -f
    }
}

function Clean-Production {
    $response = Read-Host "This will remove all containers, volumes, and images. Are you sure? (y/N)"
    if ($response -match "^[yY]([eE][sS])?$") {
        Write-Status "Cleaning up production environment..."
        docker-compose -f docker-compose.yml -f docker-compose.prod.yml down -v --rmi all
        docker system prune -f
        Write-Status "Production environment cleaned"
    } else {
        Write-Status "Cleanup cancelled"
    }
}

function Run-Migrations {
    Write-Status "Running database migrations..."
    docker-compose -f docker-compose.yml -f docker-compose.prod.yml exec api dotnet ef database update --project /src/src/Infrastructure --startup-project /src/src/Api
    Write-Status "Database migrations completed"
}

function Show-Help {
    Write-Host "ModularMonolith Production Docker Helper"
    Write-Host ""
    Write-Host "Usage: .\scripts\docker-prod.ps1 -Command [COMMAND] [-Service SERVICE]"
    Write-Host ""
    Write-Host "Commands:"
    Write-Host "  start     Start production environment"
    Write-Host "  stop      Stop production environment"
    Write-Host "  restart   Restart production environment"
    Write-Host "  logs      View logs (optionally specify service name)"
    Write-Host "  clean     Clean up all containers, volumes, and images"
    Write-Host "  migrate   Run database migrations"
    Write-Host "  help      Show this help message"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\scripts\docker-prod.ps1 -Command start"
    Write-Host "  .\scripts\docker-prod.ps1 -Command logs -Service api"
    Write-Host "  .\scripts\docker-prod.ps1 -Command clean"
}

# Main script logic
switch ($Command) {
    "start" { Start-Production }
    "stop" { Stop-Production }
    "restart" { Restart-Production }
    "logs" { Show-Logs }
    "clean" { Clean-Production }
    "migrate" { Run-Migrations }
    "help" { Show-Help }
    default {
        Write-Error "Unknown command: $Command"
        Show-Help
        exit 1
    }
}