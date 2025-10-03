#!/usr/bin/env pwsh

# Script to add migration for audit fields
# Run this from the solution root directory

Write-Host "Adding migration for audit fields..." -ForegroundColor Green

try {
    # Navigate to the API project directory (where the DbContext is referenced)
    Set-Location "src/Api"
    
    # Add the migration
    dotnet ef migrations add AddAuditFields --project ../Infrastructure --startup-project . --context ApplicationDbContext
    
    Write-Host "Migration added successfully!" -ForegroundColor Green
    Write-Host "To apply the migration, run: dotnet ef database update" -ForegroundColor Yellow
}
catch {
    Write-Error "Failed to add migration: $_"
    exit 1
}
finally {
    # Return to solution root
    Set-Location "../.."
}