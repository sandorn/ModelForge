using ModelForge.Contracts;

namespace ModelForge.Sidecar.Linking;

public static class LinkRefreshPlanner
{
    public sealed class PowerPointTarget
    {
        public string LinkId { get; init; } = string.Empty;
        public LinkTargetType TargetType { get; init; }
        public string TargetAddress { get; init; } = string.Empty;
        public int? SlideIndex { get; init; }
        public int? ShapeIndex { get; init; }
        public string? ShapeName { get; init; }
        public bool IsPrecise => SlideIndex.HasValue && (ShapeIndex.HasValue || !string.IsNullOrWhiteSpace(ShapeName));
    }

    public sealed class WordTarget
    {
        public string LinkId { get; init; } = string.Empty;
        public LinkTargetType TargetType { get; init; }
        public string TargetAddress { get; init; } = string.Empty;
        public int? FieldIndex { get; init; }
        public int? InlineShapeIndex { get; init; }
        public int? TableIndex { get; init; }
        public bool IsPrecise => FieldIndex.HasValue || InlineShapeIndex.HasValue || TableIndex.HasValue;
    }

    public sealed class RefreshPlan
    {
        public int MetadataCount { get; init; }
        public int PowerPointTargets { get; init; }
        public int WordTargets { get; init; }
        public int PrecisePowerPointTargets => PowerPointTargetObjects.Count(target => target.IsPrecise);
        public int PreciseWordTargets => WordTargetObjects.Count(target => target.IsPrecise);
        public bool RefreshPowerPoint => PowerPointTargets > 0;
        public bool RefreshWord => WordTargets > 0;
        public IReadOnlyList<string> LinkIds { get; init; } = Array.Empty<string>();
        public IReadOnlyList<PowerPointTarget> PowerPointTargetObjects { get; init; } = Array.Empty<PowerPointTarget>();
        public IReadOnlyList<WordTarget> WordTargetObjects { get; init; } = Array.Empty<WordTarget>();
    }

    public static RefreshPlan Create(IEnumerable<LinkMetadata> links)
    {
        var materialized = links
            .Where(link => !string.IsNullOrWhiteSpace(link.LinkId))
            .ToArray();
        var powerPointTargets = materialized
            .Where(IsPowerPointTarget)
            .Select(ToPowerPointTarget)
            .ToArray();
        var wordTargets = materialized
            .Where(IsWordTarget)
            .Select(ToWordTarget)
            .ToArray();

        return new RefreshPlan
        {
            MetadataCount = materialized.Length,
            PowerPointTargets = powerPointTargets.Length,
            WordTargets = wordTargets.Length,
            LinkIds = materialized.Select(link => link.LinkId).ToArray(),
            PowerPointTargetObjects = powerPointTargets,
            WordTargetObjects = wordTargets
        };
    }

    private static bool IsPowerPointTarget(LinkMetadata link) =>
        link.TargetType is LinkTargetType.PowerPointShape or LinkTargetType.PowerPointChart;

    private static bool IsWordTarget(LinkMetadata link) =>
        link.TargetType is LinkTargetType.WordInlineShape or LinkTargetType.WordTable;

    private static PowerPointTarget ToPowerPointTarget(LinkMetadata link)
    {
        var slideIndex = ExtractIndex(link.TargetAddress, "Slide");
        var targetSegment = GetTargetSegment(link.TargetAddress);
        var shapeIndex = ExtractIndex(targetSegment, "Shape") ?? ExtractIndex(targetSegment, "Chart");
        var shapeName = shapeIndex.HasValue || ExtractIndex(targetSegment, "Slide").HasValue
            ? null
            : NormalizeObjectName(targetSegment);

        return new PowerPointTarget
        {
            LinkId = link.LinkId,
            TargetType = link.TargetType,
            TargetAddress = link.TargetAddress,
            SlideIndex = slideIndex,
            ShapeIndex = shapeIndex,
            ShapeName = shapeName
        };
    }

    private static WordTarget ToWordTarget(LinkMetadata link)
    {
        return new WordTarget
        {
            LinkId = link.LinkId,
            TargetType = link.TargetType,
            TargetAddress = link.TargetAddress,
            FieldIndex = ExtractIndex(link.TargetAddress, "Field"),
            InlineShapeIndex = ExtractIndex(link.TargetAddress, "InlineShape"),
            TableIndex = ExtractIndex(link.TargetAddress, "Table")
        };
    }

    private static string GetTargetSegment(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return string.Empty;
        }

        var segments = address
            .Split(new[] { '/', '\\', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return segments.Length == 0 ? address : segments[^1];
    }

    private static string? NormalizeObjectName(string segment)
    {
        var normalized = segment.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static int? ExtractIndex(string text, string prefix)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var pattern = $@"(?:^|[/\\|\s]){System.Text.RegularExpressions.Regex.Escape(prefix)}\s*\[?\s*(\d+)\s*\]?\b";
        var match = System.Text.RegularExpressions.Regex.Match(
            text,
            pattern,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var index) && index > 0)
        {
            return index;
        }

        return null;
    }
}
