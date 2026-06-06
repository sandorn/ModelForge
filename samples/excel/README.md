# Excel Sample Files

To generate sample Excel workbooks for testing ModelForge:

```powershell
.\scripts\generate-samples.ps1
```

This creates:
| File | Description |
|------|-------------|
| `financial-model-basic.xlsx` | Three-statement financial model with Revenue, COGS, EBITDA, EBIT |
| `model-with-errors.xlsx` | Contains error values (#REF!, #DIV/0!, #N/A), hardcoded values, and normal formulas |

These samples are used for:
- Model Check (error scanning)
- Visualizations (cell type classification)
- Power Tools (IFERROR wrapping, statistics)
- Prepare to Share (sensitive data removal)