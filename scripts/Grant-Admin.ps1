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
try {
    $env:FOODSALES_ADMIN_EMAIL = $Email.Trim()
    Write-Host "Aktivizimi i administratorit te pare nga llogaria ekzistuese: $Email"
    Write-Host 'Kjo komande refuzohet nese tashme ekziston nje administrator. Per te tjeret perdorni panelin.'
    Write-Host 'Ndaleni API-ne ne Visual Studio para kesaj komande nese eshte duke punuar.'
    & $Dotnet run --project (Join-Path $backendRoot 'src/FoodSales.Api') --no-launch-profile -- --grant-admin
    if ($LASTEXITCODE -ne 0) { throw 'Dhenia e rolit Admin deshtoi. Kontrolloni mesazhin me siper.' }
    Write-Host 'Roli Admin u vendos. Fjalekalimi nuk u ndryshua. Dilni nga llogaria dhe hyni perseri.'
} finally { $env:FOODSALES_ADMIN_EMAIL = $oldEmail }
