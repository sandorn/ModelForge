namespace ModelForge.Sidecar.PowerTools;

/// <summary>
/// 公式简化助手 — 分析选定公式并给出简化建议。
/// </summary>
public static class FormulaSimplifier
{
    public sealed class SimplificationReport
    {
        public int FormulasAnalyzed { get; set; }
        public int SuggestionsFound { get; set; }
        public List<FormulaSuggestion> Suggestions { get; } = new();
    }

    public sealed class FormulaSuggestion
    {
        public string Address { get; init; } = string.Empty;
        public string CurrentFormula { get; init; } = string.Empty;
        public string Issue { get; init; } = string.Empty;
        public string? SimplifiedFormula { get; init; }
    }

    private static readonly HashSet<string> VolatileFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "OFFSET", "INDIRECT", "TODAY", "NOW", "RAND", "RANDBETWEEN",
        "CELL", "INFO", "AREAS", "COLUMNS", "ROWS", "INDEX" // INDEX is not volatile but often overused with OFFSET
    };

    public static SimplificationReport Analyze(dynamic excelApp)
    {
        var report = new SimplificationReport();
        dynamic selection = excelApp.Selection;

        foreach (dynamic cell in selection)
        {
            if (!cell.HasFormula) continue;

            string formula = (cell.Formula as string) ?? "";
            if (string.IsNullOrWhiteSpace(formula)) continue;

            report.FormulasAnalyzed++;
            string trimmed = formula.Trim();

            // 1. 冗余外层括号
            if (trimmed.StartsWith('=') && trimmed[1] == '(' && trimmed[^1] == ')')
            {
                string inner = trimmed[2..^1];
                if (IsBalanced(inner) && !inner.Contains(','))
                {
                    report.Suggestions.Add(new FormulaSuggestion
                    {
                        Address = cell.Address,
                        CurrentFormula = formula,
                        Issue = "存在冗余外层括号",
                        SimplifiedFormula = "=" + inner
                    });
                    report.SuggestionsFound++;
                }
            }

            // 2. 乘以 1 或除以 1 或加 0
            if (trimmed.Contains("*1") || trimmed.Contains("/1") || trimmed.Contains("+0"))
            {
                report.Suggestions.Add(new FormulaSuggestion
                {
                    Address = cell.Address,
                    CurrentFormula = formula,
                    Issue = "公式包含 *1、/1 或 +0 等无效运算",
                    SimplifiedFormula = null
                });
                report.SuggestionsFound++;
            }

            // 3. 双重负号 (-- 或 +-）
            if (trimmed.Contains("--") || trimmed.Contains("+-"))
            {
                report.Suggestions.Add(new FormulaSuggestion
                {
                    Address = cell.Address,
                    CurrentFormula = formula,
                    Issue = "公式包含 -- 或 +- 等冗余符号运算",
                    SimplifiedFormula = null
                });
                report.SuggestionsFound++;
            }

            // 4. 易失函数检测
            foreach (var vf in VolatileFunctions)
            {
                if (trimmed.Contains(vf, StringComparison.OrdinalIgnoreCase))
                {
                    report.Suggestions.Add(new FormulaSuggestion
                    {
                        Address = cell.Address,
                        CurrentFormula = formula,
                        Issue = $"公式使用了 {vf} 函数（易失/低效函数）。建议：评估是否可用 INDEX/MATCH 或直接引用替代。",
                        SimplifiedFormula = null
                    });
                    report.SuggestionsFound++;
                    break; // 每个公式最多一条易失函数警告
                }
            }

            // 5. 嵌套 IF 超过 3 层
            int ifCount = CountOccurrences(trimmed, "IF(");
            if (ifCount > 3)
            {
                report.Suggestions.Add(new FormulaSuggestion
                {
                    Address = cell.Address,
                    CurrentFormula = formula,
                    Issue = $"公式包含 {ifCount} 层嵌套 IF。建议：使用 IFS 函数或查找表简化。",
                    SimplifiedFormula = null
                });
                report.SuggestionsFound++;
            }

            // 6. VLOOKUP 建议改用 XLOOKUP
            if (trimmed.Contains("VLOOKUP(", StringComparison.OrdinalIgnoreCase))
            {
                report.Suggestions.Add(new FormulaSuggestion
                {
                    Address = cell.Address,
                    CurrentFormula = formula,
                    Issue = "使用了 VLOOKUP。建议：改用 XLOOKUP 获得更好的性能和灵活性。",
                    SimplifiedFormula = null
                });
                report.SuggestionsFound++;
            }
        }

        return report;
    }

    private static bool IsBalanced(string expr)
    {
        int depth = 0;
        foreach (char c in expr)
        {
            if (c == '(') depth++;
            else if (c == ')') depth--;
            if (depth < 0) return false;
        }
        return depth == 0;
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0, idx = 0;
        while ((idx = text.IndexOf(pattern, idx, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            idx += pattern.Length;
        }
        return count;
    }
}
