param(
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

$addInRoots = @(
    "HKCU:\Software\Microsoft\Office\Excel\Addins",
    "HKLM:\Software\Microsoft\Office\Excel\Addins",
    "HKLM:\Software\WOW6432Node\Microsoft\Office\Excel\Addins",
    "HKCU:\Software\Microsoft\Office\PowerPoint\Addins",
    "HKLM:\Software\Microsoft\Office\PowerPoint\Addins",
    "HKLM:\Software\WOW6432Node\Microsoft\Office\PowerPoint\Addins",
    "HKCU:\Software\Microsoft\Office\Word\Addins",
    "HKLM:\Software\Microsoft\Office\Word\Addins",
    "HKLM:\Software\WOW6432Node\Microsoft\Office\Word\Addins"
)

function Get-LegacyModelForgeAddIns {
    foreach ($root in $addInRoots) {
        if (-not (Test-Path -LiteralPath $root)) {
            continue
        }

        Get-ChildItem -LiteralPath $root | ForEach-Object {
            $properties = Get-ItemProperty -LiteralPath $_.PSPath
            $registryPath = $_.Name
            $manifest = [string]$properties.Manifest
            $friendlyName = [string]$properties.FriendlyName
            $description = [string]$properties.Description
            $identity = "$registryPath $manifest $friendlyName $description"

            if ($identity -match "ModelForge" -and ($manifest -match "src[/\\]vsto" -or $registryPath -match "\\ModelForge\\." -or $registryPath -match "\\ModelForge\.") ) {
                [pscustomobject]@{
                    RegistryPath = $registryPath
                    PsPath = $_.PSPath
                    FriendlyName = $friendlyName
                    Manifest = $manifest
                    LoadBehavior = $properties.LoadBehavior
                    Description = $description
                }
            }
        }
    }
}

$legacyAddIns = @(Get-LegacyModelForgeAddIns)
if ($legacyAddIns.Count -eq 0) {
    Write-Host "No legacy ModelForge VSTO add-ins found."
    exit 0
}

Write-Host "Legacy ModelForge VSTO add-ins:"
$legacyAddIns | Format-Table RegistryPath, LoadBehavior, Manifest -AutoSize

foreach ($addIn in $legacyAddIns) {
    if ($WhatIf) {
        Write-Host "WhatIf: Remove-Item -LiteralPath $($addIn.PsPath) -Recurse -Force"
        continue
    }

    Remove-Item -LiteralPath $addIn.PsPath -Recurse -Force
    Write-Host "Removed: $($addIn.RegistryPath)"
}
