<#
.SYNOPSIS
    Builds ModelForge publish artifacts and a WiX MSI installer.

.DESCRIPTION
    Publishes Sidecar and Backend as self-contained single-file executables,
    builds the Web Add-in, generates WiX file fragments for Web/manifest assets,
    and creates ModelForge.msi in the repository root.
#>
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$publishDir = Join-Path $root "publish"
$installerDir = Join-Path $root "installer\ModelForge.Installer"
$generatedWxs = Join-Path $installerDir "GeneratedWebFiles.wxs"
$msiPath = Join-Path $root "ModelForge.msi"
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"

function ConvertTo-WixId {
    param([string]$Value)
    $id = ($Value -replace '[^A-Za-z0-9_]', '_')
    if ($id -match '^[0-9]') {
        $id = "Id_$id"
    }
    if ($id.Length -gt 70) {
        $id = $id.Substring(0, 70)
    }
    return $id
}

function Add-WixFileComponents {
    param(
        [System.Text.StringBuilder]$Builder,
        [string]$ComponentGroupId,
        [string]$DirectoryId,
        [string]$BasePath,
        [string]$Prefix
    )

    [void]$Builder.AppendLine("    <ComponentGroup Id=`"$ComponentGroupId`" Directory=`"$DirectoryId`">")
    $index = 0
    Get-ChildItem -Path $BasePath -File -Recurse | Sort-Object FullName | ForEach-Object {
        $index++
        $baseFullPath = (Resolve-Path $BasePath).Path.TrimEnd('\') + '\'
        $relative = $_.FullName.Substring($baseFullPath.Length).Replace("\", "/")
        $fileId = ConvertTo-WixId "${Prefix}_File_${index}_$relative"
        $componentId = ConvertTo-WixId "${Prefix}_Component_${index}_$relative"
        $source = $_.FullName.Replace("&", "&amp;")
        [void]$Builder.AppendLine("      <Component Id=`"$componentId`" Guid=`"*`">")
        [void]$Builder.AppendLine("        <File Id=`"$fileId`" Source=`"$source`" KeyPath=`"yes`" />")
        [void]$Builder.AppendLine("      </Component>")
    }
    [void]$Builder.AppendLine("    </ComponentGroup>")
}

Write-Host "=== ModelForge Installer Build ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration  Runtime: $Runtime"
Write-Host "Output: $msiPath"
Write-Host ""

if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}
New-Item -ItemType Directory -Path $publishDir, "$publishDir\Web" -Force | Out-Null

Write-Host "[1/5] Publishing Sidecar..." -ForegroundColor Yellow
dotnet publish "$root\src\sidecar\ModelForge.Sidecar\ModelForge.Sidecar.csproj" `
    -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o "$publishDir\Sidecar" | Out-Null

Write-Host "[2/5] Publishing Backend..." -ForegroundColor Yellow
dotnet publish "$root\src\backend\ModelForge.Backend\ModelForge.Backend.csproj" `
    -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o "$publishDir\Backend" | Out-Null

Write-Host "[3/5] Building Web Add-in..." -ForegroundColor Yellow
Push-Location "$root\src\web"
try {
    npm run build | Out-Null
    Copy-Item -Recurse "dist\*" "$publishDir\Web" -Force
    Copy-Item "public\function-file.html" "$publishDir\Web" -Force
}
finally {
    Pop-Location
}

Copy-Item -Path "$root\manifest" -Destination "$publishDir\manifest" -Recurse -Force

Write-Host "[4/5] Generating WiX file fragments..." -ForegroundColor Yellow
$wxs = [System.Text.StringBuilder]::new()
[void]$wxs.AppendLine('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
[void]$wxs.AppendLine('  <Fragment>')
Add-WixFileComponents -Builder $wxs -ComponentGroupId "WebComponents" -DirectoryId "WebDir" -BasePath "$publishDir\Web" -Prefix "Web"
Add-WixFileComponents -Builder $wxs -ComponentGroupId "ManifestComponents" -DirectoryId "ManifestDir" -BasePath "$publishDir\manifest" -Prefix "Manifest"
[void]$wxs.AppendLine('  </Fragment>')
[void]$wxs.AppendLine('</Wix>')
Set-Content -Path $generatedWxs -Value $wxs.ToString() -Encoding UTF8

@"
{
  "product": "ModelForge",
  "version": "0.1.1",
  "configuration": "$Configuration",
  "runtime": "$Runtime",
  "builtAt": "$stamp"
}
"@ | Out-File -FilePath "$publishDir\build-info.json" -Encoding UTF8

Write-Host "[5/5] Building MSI..." -ForegroundColor Yellow
if (Test-Path $msiPath) {
    Remove-Item -Force $msiPath
}
wix build `
    "$installerDir\Package.wxs" `
    "$generatedWxs" `
    -arch x64 `
    -o "$msiPath" `
    -pdb "$root\ModelForge.wixpdb" | Out-Null

$sizeMb = [Math]::Round((Get-Item $msiPath).Length / 1MB, 2)
Write-Host ""
Write-Host "=== Build Complete ===" -ForegroundColor Green
Write-Host "MSI: $msiPath ($sizeMb MB)"
Write-Host "Publish: $publishDir"

