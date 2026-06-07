param(
    [string]$SidecarBaseUrl = "http://localhost:5200",
    [switch]$StopWps,
    [switch]$SkipSidecar
)

$ErrorActionPreference = "Stop"
$failed = $false

function Write-Step {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $Message"
}

function Get-ProcessSnapshot {
    param([Parameter(Mandatory = $true)][string[]]$Names)

    $items = foreach ($name in $Names) {
        Get-Process -Name $name -ErrorAction SilentlyContinue
    }

    $items | Sort-Object ProcessName, Id
}

function Get-SafeProcessPath {
    param([Parameter(Mandatory = $true)]$Process)

    try {
        return [string]$Process.Path
    }
    catch {
        return ""
    }
}

function Test-MicrosoftOfficePath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $true
    }

    return $Path -match "\\Microsoft Office\\" -or
        $Path -match "\\Office\\root\\Office16\\" -or
        $Path -match "\\Office16\\"
}

function Show-ProcessTable {
    param([Parameter(Mandatory = $true)]$Processes)

    $Processes |
        Select-Object Id, ProcessName, @{Name = "Path"; Expression = { Get-SafeProcessPath $_ } } |
        Format-Table -AutoSize
}

$wpsProcesses = Get-ProcessSnapshot -Names @("wps", "et", "wpp")
if ($wpsProcesses) {
    if ($StopWps) {
        Write-Step "Stopping WPS/Kingsoft Office processes"
        $wpsProcesses | Stop-Process -Force
        Start-Sleep -Milliseconds 500
        $wpsProcesses = Get-ProcessSnapshot -Names @("wps", "et", "wpp")
    }

    if ($wpsProcesses) {
        Write-Host ""
        Write-Host "Unsupported WPS/Kingsoft Office processes are running:"
        Show-ProcessTable -Processes $wpsProcesses
        Write-Host "Close WPS before testing ModelForge Ribbon commands, or rerun with -StopWps."
        $failed = $true
    }
}

$officeProcesses = Get-ProcessSnapshot -Names @("EXCEL", "POWERPNT", "WINWORD")
if ($officeProcesses) {
    Write-Step "Detected Office host processes"
    Show-ProcessTable -Processes $officeProcesses

    foreach ($process in $officeProcesses) {
        $path = Get-SafeProcessPath $process
        if (-not (Test-MicrosoftOfficePath -Path $path)) {
            Write-Host "Unsupported Office process path for $($process.ProcessName): $path"
            $failed = $true
        }
    }
}
else {
    Write-Step "No Microsoft Office host process is currently running"
}

if (-not $SkipSidecar) {
    try {
        $health = Invoke-RestMethod "$SidecarBaseUrl/health"
        Write-Step "Sidecar health: $($health.status)"
    }
    catch {
        Write-Step "Sidecar health check skipped: $($_.Exception.Message)"
    }

    try {
        $status = Invoke-RestMethod "$SidecarBaseUrl/api/status"
        if ($status.data) {
            $connected = [bool]$status.data.connected
            $version = [string]$status.data.version
            $errorMessage = [string]$status.data.error
            Write-Step "Sidecar Office status: connected=$connected version=$version"

            if ($version) {
                $majorPart = $version.Split(".")[0]
                $major = 0
                if ([int]::TryParse($majorPart, [ref]$major) -and $major -gt 0 -and $major -lt 16) {
                    Write-Host "Unsupported Office COM version detected by Sidecar: $version"
                    $failed = $true
                }
            }

            if ($errorMessage -match "WPS|Kingsoft|Office 12") {
                Write-Host "Sidecar reported unsupported Office COM binding: $errorMessage"
                $failed = $true
            }
        }
    }
    catch {
        Write-Step "Sidecar status check skipped: $($_.Exception.Message)"
    }
}

if ($failed) {
    throw "Office runtime preflight failed."
}

Write-Step "Office runtime preflight passed"
