param(
    [Parameter(Mandatory=$true)][string]$Email,
    [string]$Dotnet = ''
)
$ErrorActionPreference = 'Stop'
$backendRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (!$Dotnet) {
    $localSdk = Join-Path $env:USERPROFILE 'Documents\ChatGPT\Food-sales-app\.dotnet\dotnet.exe'
    $Dotnet = if (Test-Path -LiteralPath $localSdk) { $localSdk } else { (Get-Command dotnet -ErrorAction Stop).Source }
}
$oldEmail = $env:FOODSALES_ADMIN_EMAIL
$oldPassword = $env:FOODSALES_ADMIN_PASSWORD
$securePassword = $null
$confirmation = $null
try {
    Write-Host "Krijimi i administratorit te pare: $Email"
    Write-Host 'Pas adminit te pare, administratoret e tjere shtohen vetem nga paneli.'
    Write-Host 'Fjalekalimi: minimum 12 karaktere, shkronje e madhe/e vogel, numer dhe simbol.'
    $securePassword = Read-Host 'Vendos fjalekalimin (nuk shfaqet ne ekran)' -AsSecureString
    $confirmation = Read-Host 'Perserit fjalekalimin' -AsSecureString
    $plainPassword = [System.Net.NetworkCredential]::new('', $securePassword).Password
    $plainConfirmation = [System.Net.NetworkCredential]::new('', $confirmation).Password
    if ($plainPassword -cne $plainConfirmation) { throw 'Fjalekalimet nuk perputhen. Llogaria nuk u krijua.' }
    $env:FOODSALES_ADMIN_EMAIL = $Email.Trim()
    $env:FOODSALES_ADMIN_PASSWORD = $plainPassword
    $plainPassword = $null
    $plainConfirmation = $null
    $project = Join-Path $backendRoot 'src/FoodSales.Api'
    & $Dotnet run --project $project --no-launch-profile -- --create-admin
    if ($LASTEXITCODE -ne 0) { throw 'Krijimi i administratorit deshtoi. Kontrolloni mesazhin me siper.' }
    Write-Host 'Administratori u krijua. Mund te hyni ne /hyr me emailin dhe fjalekalimin qe vendoset.'
} finally {
    $plainPassword = $null
    $plainConfirmation = $null
    $env:FOODSALES_ADMIN_EMAIL = $oldEmail
    $env:FOODSALES_ADMIN_PASSWORD = $oldPassword
    if ($securePassword) { $securePassword.Dispose() }
    if ($confirmation) { $confirmation.Dispose() }
}
