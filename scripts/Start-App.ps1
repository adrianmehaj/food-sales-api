param([string]$Dotnet = '', [ValidateRange(1024,65535)][int]$Port = 5080)
$ErrorActionPreference = 'Stop'
$backendRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (!$Dotnet) {
    $localSdk = Join-Path $env:USERPROFILE 'Documents\ChatGPT\Food-sales-app\.dotnet\dotnet.exe'
    $Dotnet = if (Test-Path -LiteralPath $localSdk) { $localSdk } else { (Get-Command dotnet -ErrorAction Stop).Source }
}
$oldEnvironment = $env:ASPNETCORE_ENVIRONMENT
try {
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    Write-Host "Aplikacioni lokal: http://localhost:$Port. Shtyp Ctrl+C per ta ndalur."
    & $Dotnet run --project (Join-Path $backendRoot 'src/FoodSales.Api') --no-launch-profile -- --urls="http://localhost:$Port"
    if ($LASTEXITCODE -ne 0) { throw 'Nisja e aplikacionit deshtoi.' }
} finally { $env:ASPNETCORE_ENVIRONMENT = $oldEnvironment }
