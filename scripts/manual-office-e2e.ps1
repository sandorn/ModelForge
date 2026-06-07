param(
    [string]$SidecarBaseUrl = "http://localhost:5200",
    [string]$OutputDir = (Join-Path (Split-Path -Parent $PSScriptRoot) "artifacts\manual-e2e"),
    [switch]$StopWps
)

$ErrorActionPreference = "Stop"

function Invoke-SidecarCommand {
    param(
        [Parameter(Mandatory = $true)][string]$CommandId,
        [Parameter(Mandatory = $true)][ValidateSet("excel", "powerpoint", "word")][string]$OfficeHost,
        [hashtable]$Arguments = @{}
    )

    $body = @{
        commandId = $CommandId
        host = $OfficeHost
        arguments = $Arguments
    } | ConvertTo-Json -Depth 8

    $response = Invoke-RestMethod `
        -Method Post `
        -Uri "$SidecarBaseUrl/api/execute" `
        -ContentType "application/json" `
        -Body $body

    if (-not $response.data.success) {
        throw "Sidecar command failed: $CommandId / $($response.error)"
    }

    return $response
}

function Write-Step {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $Message"
}

function Close-ComObject {
    param([object]$ComObject)
    if ($null -ne $ComObject) {
        [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($ComObject)
    }
}

function Start-OfficeAndGetApplication {
    param(
        [Parameter(Mandatory = $true)][string]$ProcessName,
        [Parameter(Mandatory = $true)][string]$ComProgId
    )

    $officeRoots = @(
        "C:\Program Files\Microsoft Office\root\Office16",
        "C:\Program Files (x86)\Microsoft Office\root\Office16"
    )

    $exePath = $officeRoots |
        ForEach-Object { Join-Path $_ $ProcessName } |
        Where-Object { Test-Path $_ } |
        Select-Object -First 1

    if (-not $exePath) {
        throw "Office executable not found: $ProcessName"
    }

    Start-Process -FilePath $exePath | Out-Null

    $lastError = $null
    for ($attempt = 1; $attempt -le 20; $attempt++) {
        Start-Sleep -Milliseconds 500
        try {
            return [Runtime.InteropServices.Marshal]::GetActiveObject($ComProgId)
        }
        catch {
            $lastError = $_.Exception.Message
        }
    }

    throw "Could not attach to running Office application: $ComProgId. $lastError"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$excel = $null
$ppt = $null
$word = $null

try {
    & (Join-Path $PSScriptRoot "check-office-runtime.ps1") `
        -SidecarBaseUrl $SidecarBaseUrl `
        -StopWps:$StopWps

    $health = Invoke-RestMethod "$SidecarBaseUrl/health"
    if ($health.status -ne "Healthy") {
        throw "Sidecar health is not Healthy."
    }

    Write-Step "Launching Excel"
    $excel = Start-OfficeAndGetApplication -ProcessName "EXCEL.EXE" -ComProgId "Excel.Application"
    $excel.Visible = $true
    $excel.DisplayAlerts = $false
    $workbook = $excel.Workbooks.Add()
    $worksheet = $excel.ActiveSheet
    $worksheet.Name = "E2E"
    $worksheet.Cells.Item(1, 1).Value2 = 100
    $worksheet.Cells.Item(1, 2).Value2 = 0
    $worksheet.Cells.Item(3, 1).Formula = "=A1/B1"
    $worksheet.Range("A3").Select() | Out-Null

    $excelStatus = Invoke-RestMethod "$SidecarBaseUrl/api/status"
    if (-not $excelStatus.data.connected) {
        throw "Sidecar did not connect to visible Excel."
    }

    Write-Step "Executing Excel wrap-iferror"
    $wrapResponse = Invoke-SidecarCommand `
        -CommandId "excel.wrap-iferror" `
        -OfficeHost "excel" `
        -Arguments @{ fallback = "0" }

    $formula = [string]$worksheet.Cells.Item(3, 1).Formula
    if (-not $formula.StartsWith("=IFERROR(", [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Excel wrap-iferror did not update the visible workbook. Formula: $formula"
    }

    Write-Step "Launching PowerPoint"
    $ppt = Start-OfficeAndGetApplication -ProcessName "POWERPNT.EXE" -ComProgId "PowerPoint.Application"
    $ppt.Visible = 1
    $presentation = $ppt.Presentations.Add()
    $slide = $presentation.Slides.Add(1, 12)
    $shape1 = $slide.Shapes.AddShape(1, 120, 100, 120, 50)
    $shape2 = $slide.Shapes.AddShape(1, 320, 140, 120, 50)
    $slide.Shapes.Range(@(1, 2)).Select()

    Write-Step "Executing PowerPoint align-left"
    $alignResponse = Invoke-SidecarCommand `
        -CommandId "ppt.align-left" `
        -OfficeHost "powerpoint"

    if ([math]::Abs([double]$shape1.Left - [double]$shape2.Left) -gt 0.5) {
        throw "PowerPoint align-left did not align selected shapes."
    }

    $worksheet.Range("A1:B3").Select() | Out-Null
    Write-Step "Executing Excel to PowerPoint link"
    $linkResponse = Invoke-SidecarCommand `
        -CommandId "excel.link-to-powerpoint" `
        -OfficeHost "excel"

    if ($presentation.Slides.Count -lt 2) {
        throw "Excel to PowerPoint link did not create a target slide."
    }

    Write-Step "Launching Word"
    $word = Start-OfficeAndGetApplication -ProcessName "WINWORD.EXE" -ComProgId "Word.Application"
    $word.Visible = $true
    $word.DisplayAlerts = 0
    $document = $word.Documents.Add()

    Write-Step "Executing Word due diligence builder"
    $wordResponse = Invoke-SidecarCommand `
        -CommandId "word.build-due-diligence" `
        -OfficeHost "word" `
        -Arguments @{ companyName = "ModelForge E2E" }

    $wordText = [string]$document.Content.Text
    if (-not $wordText.Contains("ModelForge E2E")) {
        throw "Word due diligence template was not inserted into the visible document."
    }

    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $excelPath = Join-Path $OutputDir "office-e2e-$stamp.xlsx"
    $pptPath = Join-Path $OutputDir "office-e2e-$stamp.pptx"
    $wordPath = Join-Path $OutputDir "office-e2e-$stamp.txt"

    Write-Step "Saving generated Office files"
    $workbook.SaveAs($excelPath, 51)       # xlOpenXMLWorkbook
    $presentation.SaveAs($pptPath, 24)     # ppSaveAsOpenXMLPresentation
    Set-Content -LiteralPath $wordPath -Value $wordText -Encoding UTF8

    [pscustomobject]@{
        status = "passed"
        sidecar = $health.service
        excelWorkbook = $excelPath
        powerpointDeck = $pptPath
        wordDocument = $wordPath
        excelStatusTraceId = $excelStatus.traceId
        excelCommandTraceId = $wrapResponse.traceId
        powerpointCommandTraceId = $alignResponse.traceId
        linkCommandTraceId = $linkResponse.traceId
        wordCommandTraceId = $wordResponse.traceId
    } | ConvertTo-Json -Depth 8
}
finally {
    if ($null -ne $document) { $document.Close($false) }
    if ($null -ne $word) { $word.Quit() }
    if ($null -ne $presentation) { $presentation.Close() }
    if ($null -ne $ppt) { $ppt.Quit() }
    if ($null -ne $workbook) { $workbook.Close($false) }
    if ($null -ne $excel) { $excel.Quit() }

    Close-ComObject $document
    Close-ComObject $word
    Close-ComObject $presentation
    Close-ComObject $ppt
    Close-ComObject $workbook
    Close-ComObject $excel
}
