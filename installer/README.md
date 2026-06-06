# ModelForge Installer

## Prerequisites

- .NET 10 SDK
- Node.js 20+
- WiX Toolset v5: `dotnet tool install --global wix`

## Build

Run from the repository root:

```powershell
.\scripts\build-installer.ps1 -Configuration Release
```

The script performs the full packaging pipeline:

- Publishes Backend as a self-contained single-file `win-x64` executable.
- Publishes Sidecar as a self-contained single-file `win-x64` executable.
- Builds the Web Add-in and copies `function-file.html`.
- Copies the Office manifest.
- Generates `installer\ModelForge.Installer\GeneratedWebFiles.wxs`.
- Builds `ModelForge.msi` in the repository root.

## Output

| Artifact | Purpose |
| --- | --- |
| `ModelForge.msi` | Installable MSI package |
| `ModelForge.wixpdb` | WiX debug database |
| `publish\Backend\ModelForge.Backend.exe` | Backend service executable |
| `publish\Sidecar\ModelForge.Sidecar.exe` | Sidecar service executable |
| `publish\Web\` | Web Add-in static files |
| `publish\manifest\modelForge.web.xml` | Office Add-in manifest |

The MSI installs files under `C:\Program Files\ModelForge\` and registers `ModelForge.Backend` and `ModelForge.Sidecar` as Windows Services.
Both services are built with Windows Service lifetime support and also remain runnable from a console for local smoke tests.

## Validate

```powershell
wix msi validate ModelForge.msi
```

To inspect embedded files:

```powershell
$out = Join-Path $env:TEMP "modelforge-msi-decompile"
Remove-Item $out -Recurse -Force -ErrorAction SilentlyContinue
wix msi decompile ModelForge.msi -x $out
Get-ChildItem $out\File
```

## Install And Uninstall

Run these commands from an elevated PowerShell session.

Interactive install:

```powershell
msiexec /i ModelForge.msi /l*v install.log
```

Quiet install:

```powershell
msiexec /i ModelForge.msi /quiet /l*v install.log
```

Uninstall:

```powershell
msiexec /x ModelForge.msi /l*v uninstall.log
```

Post-install checks:

```powershell
sc query ModelForge.Sidecar
sc query ModelForge.Backend
curl http://localhost:5200/health
curl http://localhost:5095/health
Get-ChildItem "C:\Program Files\ModelForge" -Recurse
```

Office Add-in sideloading is still manual: copy or register `C:\Program Files\ModelForge\manifest\modelForge.web.xml` through an Office trusted catalog.
