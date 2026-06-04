param(
    [string]$Subject = "CN=ModelForge VSTO Development",
    [int]$Years = 3
)

$ErrorActionPreference = "Stop"

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$propsPath = Join-Path $projectRoot "src\vsto\ModelForge.Excel\ModelForge.Excel.LocalSigning.props"

$certificate = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
    Where-Object { $_.Subject -eq $Subject -and $_.NotAfter -gt (Get-Date).AddDays(30) } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1

if ($null -eq $certificate) {
    $certificate = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $Subject `
        -CertStoreLocation Cert:\CurrentUser\My `
        -KeyUsage DigitalSignature `
        -KeyExportPolicy Exportable `
        -NotAfter (Get-Date).AddYears($Years)
}

$xml = @"
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <SignManifests>true</SignManifests>
    <ManifestCertificateThumbprint>$($certificate.Thumbprint)</ManifestCertificateThumbprint>
  </PropertyGroup>
</Project>
"@

Set-Content -Path $propsPath -Value $xml -Encoding UTF8

Write-Host "ModelForge VSTO development signing props created: $propsPath"
Write-Host "Certificate subject: $($certificate.Subject)"
Write-Host "Certificate thumbprint: $($certificate.Thumbprint)"