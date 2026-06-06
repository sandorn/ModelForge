# ModelForge Sample Generator
# Requires Excel, PowerPoint, and Word to be installed.
# Run: .\generate-samples.ps1
# Output: samples/excel/*.xlsx, samples/powerpoint/*.pptx, samples/word/*.docx

$ErrorActionPreference = "Stop"
Write-Host "=== ModelForge Sample Generator ===" -ForegroundColor Cyan

# ── Excel: Financial Model ──
Write-Host "Generating Excel samples..." -ForegroundColor Yellow
$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false

# --- financial-model-basic.xlsx ---
$wb1 = $excel.Workbooks.Add()
$ws1 = $wb1.Worksheets(1)
$ws1.Name = "Income Statement"

$headers = @("Item", 2021, 2022, 2023, 2024, 2025)
for ($c = 0; $c -lt $headers.Count; $c++) {
    $ws1.Cells(1, $c + 1).Value = $headers[$c]
    $ws1.Cells(1, $c + 1).Font.Bold = $true
}
$ws1.Cells(2, 1).Value = "Revenue"
$ws1.Cells(2, 2).Formula = "=1000+RAND()*200"
$ws1.Range("C2:F2").FormulaR1C1 = "=RC[-1]*(1+0.1+RAND()*0.05)"  # 10-15% growth
$ws1.Cells(3, 1).Value = "COGS"
$ws1.Range("B3:F3").FormulaR1C1 = "=R[-1]C*0.6"
$ws1.Cells(4, 1).Value = "Gross Profit"
$ws1.Range("B4:F4").FormulaR1C1 = "=R[-2]C-R[-1]C"
$ws1.Cells(5, 1).Value = "Operating Expenses"
$ws1.Range("B5:F5").FormulaR1C1 = "=R[-3]C*0.15"
$ws1.Cells(6, 1).Value = "EBITDA"
$ws1.Range("B6:F6").FormulaR1C1 = "=R[-2]C-R[-1]C"
$ws1.Cells(7, 1).Value = "Depreciation"
$ws1.Range("B7:F7").FormulaR1C1 = "=R[-5]C*0.05"
$ws1.Cells(8, 1).Value = "EBIT"
$ws1.Range("B8:F8").FormulaR1C1 = "=R[-2]C-R[-1]C"
$ws1.Columns(1).ColumnWidth = 20
for ($c = 2; $c -le 6; $c++) { $ws1.Columns($c).ColumnWidth = 14 }
$ws1.Range("B2:F8").NumberFormat = "#,##0"

$sampleDir = Join-Path $PSScriptRoot "samples\excel"
New-Item -ItemType Directory -Force -Path $sampleDir | Out-Null
$wb1.SaveAs((Join-Path $sampleDir "financial-model-basic.xlsx"), 51)  # 51 = xlOpenXMLWorkbook
$wb1.Close()
Write-Host "  -> samples/excel/financial-model-basic.xlsx" -ForegroundColor Green

# --- model-with-errors.xlsx ---
$wb2 = $excel.Workbooks.Add()
$ws2 = $wb2.Worksheets(1)
$ws2.Name = "Errors"

# Error values
$ws2.Cells(1, 1).Value = "#REF!"
$ws2.Cells(2, 1).Formula = "=1/0"       # #DIV/0!
$ws2.Cells(3, 1).Formula = "=VLOOKUP(1,A1,2,FALSE)"  # #N/A
$ws2.Cells(4, 1).Value = "Hardcoded 100"
$ws2.Cells(5, 1).Value = 999
$ws2.Cells(6, 1).Value = "Plain text"
$ws2.Cells(7, 1).Formula = "=B1+C1"     # normal formula

$sampleDir = Join-Path $PSScriptRoot "samples\excel"
$wb2.SaveAs((Join-Path $sampleDir "model-with-errors.xlsx"), 51)
$wb2.Close()
Write-Host "  -> samples/excel/model-with-errors.xlsx" -ForegroundColor Green

$excel.Quit()
[System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null

# ── PowerPoint: Investment Committee Template ──
Write-Host "Generating PowerPoint sample..." -ForegroundColor Yellow
$ppt = New-Object -ComObject PowerPoint.Application
$ppt.Visible = $false

$pres = $ppt.Presentations.Add()
# Slide 1: Title
$slide1 = $pres.Slides.Add(1, 1)  # ppLayoutTitle
$slide1.Shapes(1).TextFrame.TextRange.Text = "Investment Committee Memo"
$slide1.Shapes(2).TextFrame.TextRange.Text = "Q4 2025 Review`nModelForge Demo"

# Slide 2: Agenda
$slide2 = $pres.Slides.Add(2, 2)  # ppLayoutText
$slide2.Shapes(1).TextFrame.TextRange.Text = "Agenda"
$slide2.Shapes(2).TextFrame.TextRange.Text = "1. Executive Summary`n2. Financial Performance`n3. Valuation`n4. Risks & Mitigations"

# Slide 3: Content placeholder
$slide3 = $pres.Slides.Add(3, 2)
$slide3.Shapes(1).TextFrame.TextRange.Text = "Financial Highlights"
$slide3.Shapes(2).TextFrame.TextRange.Text = "Revenue growth: 12% YoY`nEBITDA margin: 28%`nFree cash flow: $150M"

$sampleDir = Join-Path $PSScriptRoot "samples\powerpoint"
New-Item -ItemType Directory -Force -Path $sampleDir | Out-Null
$pres.SaveAs((Join-Path $sampleDir "investment-committee-template.pptx"), 1)  # 1 = ppSaveAsDefault
$pres.Close()
$ppt.Quit()
[System.Runtime.InteropServices.Marshal]::ReleaseComObject($ppt) | Out-Null
Write-Host "  -> samples/powerpoint/investment-committee-template.pptx" -ForegroundColor Green

# ── Word: Due Diligence Template ──
Write-Host "Generating Word sample..." -ForegroundColor Yellow
$word = New-Object -ComObject Word.Application
$word.Visible = $false

$doc = $word.Documents.Add()
$selection = $word.Selection
$selection.Style = "Title"
$selection.TypeText("Due Diligence Report")
$selection.TypeParagraph()

$selection.Style = "Heading 1"
$selection.TypeText("1. Executive Summary")
$selection.TypeParagraph()
$selection.Style = "Normal"
$selection.TypeText("This report summarizes the financial, legal, and operational due diligence conducted for the proposed acquisition of Target Company. Key findings are presented below.")
$selection.TypeParagraph()
$selection.TypeParagraph()

$selection.Style = "Heading 1"
$selection.TypeText("2. Financial Analysis")
$selection.TypeParagraph()
$selection.Style = "Heading 2"
$selection.TypeText("2.1 Revenue Quality")
$selection.TypeParagraph()
$selection.TypeText("Revenue is primarily recurring (85% subscription-based). Customer concentration: top 3 clients represent 12% of total revenue.")
$selection.TypeParagraph()
$selection.TypeParagraph()

$selection.Style = "Heading 2"
$selection.TypeText("2.2 EBITDA Adjustments")
$selection.TypeParagraph()
$selection.TypeText("Normalized EBITDA after adjustments: one-time restructuring costs ($2.3M), non-recurring litigation settlement ($0.8M).")

$sampleDir = Join-Path $PSScriptRoot "samples\word"
New-Item -ItemType Directory -Force -Path $sampleDir | Out-Null
$doc.SaveAs((Join-Path $sampleDir "due-diligence-template.docx"), 16)  # 16 = wdFormatXMLDocument
$doc.Close()
$word.Quit()
[System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
Write-Host "  -> samples/word/due-diligence-template.docx" -ForegroundColor Green

Write-Host "=== All samples generated ===" -ForegroundColor Cyan
Write-Host "Run 'dir samples -Recurse' to verify." -ForegroundColor Gray