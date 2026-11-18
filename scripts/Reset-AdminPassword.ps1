param(
    [Parameter(Mandatory = $true)][string]$Email,
    [string]$Dotnet = ''
)
$ErrorActionPreference = 'Stop'
$backendRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (!$Dotnet) {
    $localSdk = Join-Path $env:USERPROFILE 'Documents\ChatGPT\Food-sales-app\.dotnet\dotnet.exe'
    $Dotnet = if (Test-Path -LiteralPath $localSdk) { $localSdk } else { (Get-Command dotnet -ErrorAction Stop).Source }
}
$project = Join-Path $backendRoot 'src/FoodSales.Api'
$output = Join-Path $backendRoot 'artifacts/admin-recovery'
& $Dotnet build $project --configuration Release --output $output --disable-build-servers -m:1
if ($LASTEXITCODE -ne 0) { throw 'Build failed. No password was changed.' }
$securePassword = $null
$confirmation = $null
$oldEnvironment = $env:ASPNETCORE_ENVIRONMENT
$oldDotnetEnvironment = $env:DOTNET_ENVIRONMENT
try {
    Write-Host "Rivendosja lokale e fjalekalimit te adminit: $Email"
    Write-Host 'Minimum 12 karaktere: shkronje e madhe, e vogel, numer dhe simbol.'
    $securePassword = Read-Host 'Fjalekalimi i ri (nuk shfaqet)' -AsSecureString
    $confirmation = Read-Host 'Perserite fjalekalimin e ri' -AsSecureString
    $plainPassword = [System.Net.NetworkCredential]::new('', $securePassword).Password
    $plainConfirmation = [System.Net.NetworkCredential]::new('', $confirmation).Password
    if ($plainPassword -cne $plainConfirmation) { throw 'Fjalekalimet nuk perputhen. Asnje ndryshim nuk u be.' }
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:DOTNET_ENVIRONMENT = 'Development'
    $payload = @{ Email = $Email.Trim(); Password = $plainPassword } | ConvertTo-Json -Compress
    # stdin keeps the password out of process arguments, Git and shell history.
    $payload | & $Dotnet (Join-Path $output 'FoodSales.Api.dll') --contentRoot $project --reset-admin-password
    if ($LASTEXITCODE -ne 0) { throw 'Rivendosja deshtoi. Kontrolloni mesazhin me siper.' }
    Write-Host 'U krye. Hyni ne Tryeza me emailin dhe fjalekalimin e ri.'
} finally {
    $env:ASPNETCORE_ENVIRONMENT = $oldEnvironment
    $env:DOTNET_ENVIRONMENT = $oldDotnetEnvironment
    $payload = $null
    $plainPassword = $null
    $plainConfirmation = $null
    if ($securePassword) { $securePassword.Dispose() }
    if ($confirmation) { $confirmation.Dispose() }
}
