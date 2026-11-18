param(
    [Parameter(Mandatory = $true)][string]$SenderEmail,
    [Parameter(Mandatory = $true)][string]$PublicAppUrl,
    [string]$SmtpHost = 'smtp.gmail.com',
    [int]$SmtpPort = 587,
    [string]$Username = '',
    [string]$Dotnet = ''
)
$ErrorActionPreference = 'Stop'
$backendRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $backendRoot 'src/FoodSales.Api'
if (!$Dotnet) {
    $localSdk = Join-Path $env:USERPROFILE 'Documents\ChatGPT\Food-sales-app\.dotnet\dotnet.exe'
    $Dotnet = if (Test-Path -LiteralPath $localSdk) { $localSdk } else { (Get-Command dotnet -ErrorAction Stop).Source }
}
$address = [System.Net.Mail.MailAddress]::new($SenderEmail)
$appUri = [Uri]::new($PublicAppUrl, [UriKind]::Absolute)
if ($appUri.Scheme -ne 'https' -and !($appUri.Scheme -eq 'http' -and $appUri.IsLoopback)) { throw 'URL-ja duhet te jete HTTPS ose HTTP loopback per zhvillim.' }
if ($appUri.AbsolutePath -ne '/' -or $appUri.Query -or $appUri.Fragment -or $appUri.UserInfo) { throw 'Vendosni vetem origjinen e aplikacionit, pa shteg ose parametra.' }
if (!$Username) { $Username = $address.Address }
$securePassword = $null
try {
    Write-Host 'Konfigurimi i emailit per rivendosjen e fjalekalimit (Development).'
    Write-Host 'Per Gmail perdorni App Password; mos perdorni fjalekalimin kryesor.'
    $securePassword = Read-Host 'Kredenciali SMTP (nuk shfaqet)' -AsSecureString
    $plainPassword = [System.Net.NetworkCredential]::new('', $securePassword).Password
    if ([string]::IsNullOrWhiteSpace($plainPassword)) { throw 'Kredenciali nuk mund te jete bosh.' }
    $configuration = @{
        'Email:DeliveryMethod' = 'Smtp'
        'Email:SmtpHost' = $SmtpHost
        'Email:SmtpPort' = $SmtpPort.ToString()
        'Email:FromAddress' = $address.Address
        'Email:Username' = $Username
        'Email:Password' = $plainPassword
        'PasswordRecovery:PublicAppUrl' = $appUri.GetLeftPart([UriPartial]::Authority)
    }
    # stdin keeps the credential out of process arguments and shell history.
    $configuration | ConvertTo-Json -Compress | & $Dotnet user-secrets set --project $project
    if ($LASTEXITCODE -ne 0) { throw 'Konfigurimi nuk u ruajt.' }
    Write-Host 'U ruajt ne user secrets. Rinisni backend-in per ta aktivizuar.'
} finally {
    if ($configuration) { $configuration.Clear() }
    $plainPassword = $null
    if ($securePassword) { $securePassword.Dispose() }
}
