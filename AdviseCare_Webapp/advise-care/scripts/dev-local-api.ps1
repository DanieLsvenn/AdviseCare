param(
  [int]$Port = 3000
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$certificate = Get-ChildItem Cert:\CurrentUser\My |
  Where-Object {
    $_.FriendlyName -eq "ASP.NET Core HTTPS development certificate" -and
    $_.Subject -eq "CN=localhost" -and
    $_.NotAfter -gt (Get-Date)
  } |
  Sort-Object NotAfter -Descending |
  Select-Object -First 1

if (-not $certificate) {
  throw "A valid ASP.NET Core localhost development certificate was not found. Run 'dotnet dev-certs https --trust' and retry."
}

$certificatePath = Join-Path $env:TEMP "advise-care-dev-cert-$PID.pem"
$encodedCertificate = [Convert]::ToBase64String(
  $certificate.RawData,
  [Base64FormattingOptions]::InsertLineBreaks
)
$certificatePem = "-----BEGIN CERTIFICATE-----`r`n$encodedCertificate`r`n-----END CERTIFICATE-----`r`n"
[System.IO.File]::WriteAllText(
  $certificatePath,
  $certificatePem,
  [System.Text.Encoding]::ASCII
)

$originalNodeExtraCaCerts = $env:NODE_EXTRA_CA_CERTS
$exitCode = 0

try {
  Push-Location $projectRoot
  $env:NODE_EXTRA_CA_CERTS = $certificatePath
  pnpm exec next dev --port $Port
  $exitCode = $LASTEXITCODE
} finally {
  Pop-Location
  if ($null -eq $originalNodeExtraCaCerts) {
    Remove-Item Env:NODE_EXTRA_CA_CERTS -ErrorAction SilentlyContinue
  } else {
    $env:NODE_EXTRA_CA_CERTS = $originalNodeExtraCaCerts
  }
  Remove-Item -LiteralPath $certificatePath -ErrorAction SilentlyContinue
}

exit $exitCode
