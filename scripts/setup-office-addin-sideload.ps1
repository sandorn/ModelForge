param(
    [string]$CatalogPath = (Join-Path (Split-Path -Parent $PSScriptRoot) "artifacts\office-addin-catalog"),
    [string]$ManifestPath = (Join-Path (Split-Path -Parent $PSScriptRoot) "manifest\modelForge.web.xml"),
    [string]$ShareName = "ModelForgeOfficeAddins"
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $Message"
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function New-OfficeTrustedCatalog {
    param([Parameter(Mandatory = $true)][string]$CatalogUrl)

    $trustedCatalogRoot = "HKCU:\Software\Microsoft\Office\16.0\WEF\TrustedCatalogs"
    New-Item -Path $trustedCatalogRoot -Force | Out-Null

    Get-ChildItem -Path $trustedCatalogRoot -ErrorAction SilentlyContinue |
        Where-Object {
            $properties = Get-ItemProperty -LiteralPath $_.PSPath
            [string]$properties.Url -match "\\ModelForgeOfficeAddins$" -or
                [string]$properties.CatalogUrl -match "\\ModelForgeOfficeAddins$"
        } |
        ForEach-Object {
            Remove-Item -LiteralPath $_.PSPath -Recurse -Force
            Write-Step "Removed previous ModelForge trusted catalog: $($_.PSChildName)"
        }

    $existingCatalog = Get-ChildItem -Path $trustedCatalogRoot -ErrorAction SilentlyContinue |
        Where-Object {
            $properties = Get-ItemProperty -LiteralPath $_.PSPath
            [string]$properties.Url -eq $CatalogUrl
        } |
        Select-Object -First 1

    if ($existingCatalog) {
        Set-ItemProperty -LiteralPath $existingCatalog.PSPath -Name "Flags" -Type DWord -Value 1
        New-ItemProperty -LiteralPath $existingCatalog.PSPath -Name "CatalogUrl" -PropertyType String -Value $CatalogUrl -Force | Out-Null
        Write-Step "Trusted catalog already exists: $CatalogUrl"
        return
    }

    $catalogId = "{0}" -f ([guid]::NewGuid().ToString("B").ToUpperInvariant())
    $catalogKey = Join-Path $trustedCatalogRoot $catalogId
    New-Item -Path $catalogKey -Force | Out-Null
    New-ItemProperty -Path $catalogKey -Name "Id" -PropertyType String -Value $catalogId -Force | Out-Null
    New-ItemProperty -Path $catalogKey -Name "Url" -PropertyType String -Value $CatalogUrl -Force | Out-Null
    New-ItemProperty -Path $catalogKey -Name "CatalogUrl" -PropertyType String -Value $CatalogUrl -Force | Out-Null
    New-ItemProperty -Path $catalogKey -Name "Flags" -PropertyType DWord -Value 1 -Force | Out-Null
    Write-Step "Trusted catalog registered: $CatalogUrl"
}

$resolvedManifest = (Resolve-Path -LiteralPath $ManifestPath).Path
$resolvedCatalog = if (Test-Path -LiteralPath $CatalogPath) {
    (Resolve-Path -LiteralPath $CatalogPath).Path
}
else {
    (New-Item -ItemType Directory -Force -Path $CatalogPath).FullName
}

Copy-Item -LiteralPath $resolvedManifest -Destination (Join-Path $resolvedCatalog "modelForge.web.xml") -Force
Write-Step "Manifest copied to: $resolvedCatalog"

$share = Get-SmbShare -Name $ShareName -ErrorAction SilentlyContinue
if ($null -eq $share) {
    if (-not (Test-IsAdministrator)) {
        throw "Creating a local Office add-in catalog share requires an elevated PowerShell session."
    }

    New-SmbShare -Name $ShareName -Path $resolvedCatalog -ReadAccess "Everyone" | Out-Null
    Write-Step "SMB share created: \\$env:COMPUTERNAME\$ShareName"
}
elseif ((Resolve-Path -LiteralPath $share.Path).Path -ne $resolvedCatalog) {
    throw "SMB share '$ShareName' already exists but points to '$($share.Path)', not '$resolvedCatalog'."
}
else {
    if (Test-IsAdministrator) {
        Grant-SmbShareAccess -Name $ShareName -AccountName "Everyone" -AccessRight Read -Force | Out-Null
    }
    Write-Step "SMB share already exists: \\$env:COMPUTERNAME\$ShareName"
}

$catalogUrl = "\\$env:COMPUTERNAME\$ShareName"
New-OfficeTrustedCatalog -CatalogUrl $catalogUrl

Write-Host ""
Write-Host "Office sideload catalog is ready."
Write-Host "Catalog URL : $catalogUrl"
Write-Host "Manifest    : $resolvedCatalog\modelForge.web.xml"
Write-Host ""
Write-Host "Next steps:"
Write-Host "1. Close Excel, PowerPoint, Word, and WPS/Kingsoft Office completely."
Write-Host "2. Optional: run .\scripts\check-office-runtime.ps1 -StopWps to verify the runtime."
Write-Host "3. Reopen Microsoft Office 2016+/Office 2024 Excel."
Write-Host "4. Go to Home > Add-ins > More Add-ins / Advanced > SHARED FOLDER."
Write-Host "5. Select ModelForge and choose Add."
