param(
    [Parameter(Mandatory=$true)][string]$FrontendPath,
    [string]$OutputPath = '',
    [string]$Dotnet = 'dotnet'
)
$ErrorActionPreference = 'Stop'
$backendRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$frontendRoot = (Resolve-Path -LiteralPath $FrontendPath).Path
if (!(Test-Path -LiteralPath (Join-Path $frontendRoot 'scripts/copy-to-api.mjs'))) { throw 'Select the food-sales-web checkout.' }
if (!$OutputPath) { $OutputPath = Join-Path $backendRoot 'artifacts/publish' }
Push-Location $frontendRoot
try {
    & npm ci
    if ($LASTEXITCODE -ne 0) { throw 'Frontend dependency installation failed.' }
    & npm run build
    if ($LASTEXITCODE -ne 0) { throw 'Frontend build failed.' }
    & node scripts/copy-to-api.mjs $backendRoot
    if ($LASTEXITCODE -ne 0) { throw 'Frontend packaging failed.' }
} finally { Pop-Location }
& $Dotnet publish (Join-Path $backendRoot 'src/FoodSales.Api/FoodSales.Api.csproj') -c Release -o $OutputPath
if ($LASTEXITCODE -ne 0) { throw 'Backend publication failed.' }
Write-Host "Complete app published to $OutputPath. Configure SQL Server, HTTPS, local timezone and persistent Data Protection keys before deployment."
