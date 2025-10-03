# PowerShell script to verify database migrations
param(
    [string]$ProjectPath = "../Api",
    [string]$InfrastructurePath = "../Infrastructure"
)

Write-Host "=== Database Migration Verification Script ===" -ForegroundColor Green
Write-Host ""

# Change to the API project directory
Push-Location $ProjectPath

try {
    Write-Host "1. Building the project..." -ForegroundColor Yellow
    $buildResult = dotnet build --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Build successful" -ForegroundColor Green
    Write-Host ""

    Write-Host "2. Checking EF Core tools..." -ForegroundColor Yellow
    $efVersion = dotnet ef --version 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ EF Core tools not available" -ForegroundColor Red
        Write-Host "Install with: dotnet tool install --global dotnet-ef" -ForegroundColor Yellow
        exit 1
    }
    Write-Host "✅ EF Core tools available: $($efVersion.Split([Environment]::NewLine)[0])" -ForegroundColor Green
    Write-Host ""

    Write-Host "3. Listing available migrations..." -ForegroundColor Yellow
    $migrations = dotnet ef migrations list --project $InfrastructurePath --startup-project . --no-connect 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Available migrations:" -ForegroundColor Green
        $migrations | ForEach-Object { 
            if ($_ -match "^\d{14}_") {
                Write-Host "   - $_" -ForegroundColor Cyan
            }
        }
    } else {
        Write-Host "⚠️  Could not list migrations (database connection may be required)" -ForegroundColor Yellow
    }
    Write-Host ""

    Write-Host "4. Checking migration files..." -ForegroundColor Yellow
    $migrationPath = Join-Path $InfrastructurePath "Data\Migrations"
    if (Test-Path $migrationPath) {
        $migrationFiles = Get-ChildItem $migrationPath -Filter "*.cs" | Where-Object { $_.Name -match "^\d{14}_" }
        if ($migrationFiles.Count -gt 0) {
            Write-Host "✅ Found $($migrationFiles.Count) migration file(s):" -ForegroundColor Green
            $migrationFiles | ForEach-Object {
                Write-Host "   - $($_.Name)" -ForegroundColor Cyan
            }
        } else {
            Write-Host "❌ No migration files found" -ForegroundColor Red
        }
    } else {
        Write-Host "❌ Migration directory not found: $migrationPath" -ForegroundColor Red
    }
    Write-Host ""

    Write-Host "5. Checking ApplicationDbContext..." -ForegroundColor Yellow
    $contextPath = Join-Path $InfrastructurePath "Data\ApplicationDbContext.cs"
    if (Test-Path $contextPath) {
        Write-Host "✅ ApplicationDbContext found" -ForegroundColor Green
    } else {
        Write-Host "❌ ApplicationDbContext not found" -ForegroundColor Red
    }
    Write-Host ""

    Write-Host "6. Checking DatabaseMigrationService..." -ForegroundColor Yellow
    $servicePath = Join-Path $InfrastructurePath "Services\DatabaseMigrationService.cs"
    if (Test-Path $servicePath) {
        Write-Host "✅ DatabaseMigrationService found" -ForegroundColor Green
    } else {
        Write-Host "❌ DatabaseMigrationService not found" -ForegroundColor Red
    }
    Write-Host ""

    Write-Host "7. Validating connection string configuration..." -ForegroundColor Yellow
    $appsettingsPath = "appsettings.json"
    if (Test-Path $appsettingsPath) {
        $appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
        if ($appsettings.ConnectionStrings -and $appsettings.ConnectionStrings.DefaultConnection) {
            Write-Host "✅ Connection string configured" -ForegroundColor Green
            Write-Host "   Connection: $($appsettings.ConnectionStrings.DefaultConnection)" -ForegroundColor Cyan
        } else {
            Write-Host "❌ Connection string not found in appsettings.json" -ForegroundColor Red
        }
    } else {
        Write-Host "❌ appsettings.json not found" -ForegroundColor Red
    }
    Write-Host ""

    Write-Host "=== Migration Verification Complete ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "To apply migrations when database is available:" -ForegroundColor Yellow
    Write-Host "  dotnet ef database update --project $InfrastructurePath --startup-project ." -ForegroundColor Cyan
    Write-Host ""
    Write-Host "To create a new migration:" -ForegroundColor Yellow
    Write-Host "  dotnet ef migrations add <MigrationName> --project $InfrastructurePath --startup-project ." -ForegroundColor Cyan

} finally {
    Pop-Location
}