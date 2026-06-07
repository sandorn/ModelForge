namespace ModelForge.Sidecar.PowerPoint;

/// <summary>
/// PPT 形状批量操作工具。对齐、分布、统一尺寸。
/// </summary>
public static class ShapeTools
{
    private const int PpSelectionShapes = 2;

    /// <summary>
    /// 将选中的形状左对齐。
    /// </summary>
    public static string AlignLeft(dynamic pptApp) => Align(pptApp, ShapeAlignment.Left, "左对齐");

    public static string AlignCenter(dynamic pptApp) => Align(pptApp, ShapeAlignment.Center, "水平居中对齐");

    public static string AlignRight(dynamic pptApp) => Align(pptApp, ShapeAlignment.Right, "右对齐");

    public static string AlignTop(dynamic pptApp) => Align(pptApp, ShapeAlignment.Top, "顶端对齐");

    public static string AlignMiddle(dynamic pptApp) => Align(pptApp, ShapeAlignment.Middle, "垂直居中对齐");

    public static string AlignBottom(dynamic pptApp) => Align(pptApp, ShapeAlignment.Bottom, "底端对齐");

    /// <summary>
    /// 将选中的形状在水平方向上均匀分布。
    /// </summary>
    public static string DistributeHorizontal(dynamic pptApp) =>
        Distribute(pptApp, DistributionAxis.Horizontal, "水平均分");

    /// <summary>
    /// 将选中的形状在垂直方向上均匀分布。
    /// </summary>
    public static string DistributeVertical(dynamic pptApp) =>
        Distribute(pptApp, DistributionAxis.Vertical, "垂直均分");

    public static string UnifyWidth(dynamic pptApp)
    {
        var result = GetShapeRange(pptApp, minimumCount: 2);
        if (result.Error != null) return result.Error;
        dynamic shapes = result.Shapes;

        float width = shapes[1].Width;
        for (int i = 2; i <= shapes.Count; i++)
            shapes[i].Width = width;

        return $"已将 {shapes.Count} 个形状统一宽度为 {width}。";
    }

    public static string UnifyHeight(dynamic pptApp)
    {
        var result = GetShapeRange(pptApp, minimumCount: 2);
        if (result.Error != null) return result.Error;
        dynamic shapes = result.Shapes;

        float height = shapes[1].Height;
        for (int i = 2; i <= shapes.Count; i++)
            shapes[i].Height = height;

        return $"已将 {shapes.Count} 个形状统一高度为 {height}。";
    }

    /// <summary>
    /// 将选中形状统一为相同宽度（以第一个形状为准）。
    /// </summary>
    public static string UnifySize(dynamic pptApp)
    {
        var result = GetShapeRange(pptApp, minimumCount: 2);
        if (result.Error != null) return result.Error;
        dynamic shapes = result.Shapes;

        float width = shapes[1].Width;
        float height = shapes[1].Height;

        for (int i = 2; i <= shapes.Count; i++)
        {
            shapes[i].Width = width;
            shapes[i].Height = height;
        }

        return $"已将 {shapes.Count} 个形状统一为 {width}x{height}。";
    }

    private static string Align(dynamic pptApp, ShapeAlignment alignment, string label)
    {
        var result = GetShapeRange(pptApp, minimumCount: 2);
        if (result.Error != null) return result.Error;
        var shapes = GetShapeItems(result.Shapes);

        var boxes = GetShapeBoxes(shapes);
        ApplyShapeBoxes(shapes, AlignBoxes(boxes, alignment));

        return $"已将 {shapes.Count} 个形状{label}。";
    }

    private static string Distribute(dynamic pptApp, DistributionAxis axis, string label)
    {
        var result = GetShapeRange(pptApp, minimumCount: 3);
        if (result.Error != null) return result.Error;
        var shapes = GetShapeItems(result.Shapes);

        var boxes = GetShapeBoxes(shapes);
        ApplyShapeBoxes(shapes, DistributeBoxes(boxes, axis));
        return $"已将 {shapes.Count} 个形状{label}。";
    }

    public static IReadOnlyList<ShapeBox> AlignBoxes(IReadOnlyList<ShapeBox> shapes, ShapeAlignment alignment)
    {
        if (shapes.Count == 0) return Array.Empty<ShapeBox>();

        var bounds = ShapeBounds.From(shapes);
        return shapes.Select(shape => alignment switch
        {
            ShapeAlignment.Left => shape with { Left = bounds.Left },
            ShapeAlignment.Center => shape with { Left = bounds.CenterX - (shape.Width / 2f) },
            ShapeAlignment.Right => shape with { Left = bounds.Right - shape.Width },
            ShapeAlignment.Top => shape with { Top = bounds.Top },
            ShapeAlignment.Middle => shape with { Top = bounds.CenterY - (shape.Height / 2f) },
            ShapeAlignment.Bottom => shape with { Top = bounds.Bottom - shape.Height },
            _ => shape
        }).ToArray();
    }

    public static IReadOnlyList<ShapeBox> DistributeBoxes(IReadOnlyList<ShapeBox> shapes, DistributionAxis axis)
    {
        if (shapes.Count < 3) return shapes.ToArray();

        var result = shapes.ToArray();
        if (axis == DistributionAxis.Horizontal)
        {
            var ordered = shapes
                .Select((shape, index) => new { shape, index })
                .OrderBy(item => item.shape.Left)
                .ThenBy(item => item.index)
                .ToArray();
            var bounds = ShapeBounds.From(shapes);
            var totalWidth = ordered.Sum(item => item.shape.Width);
            var gap = (bounds.Right - bounds.Left - totalWidth) / (ordered.Length - 1);
            var left = bounds.Left;

            foreach (var item in ordered)
            {
                result[item.index] = item.shape with { Left = left };
                left += item.shape.Width + gap;
            }
        }
        else
        {
            var ordered = shapes
                .Select((shape, index) => new { shape, index })
                .OrderBy(item => item.shape.Top)
                .ThenBy(item => item.index)
                .ToArray();
            var bounds = ShapeBounds.From(shapes);
            var totalHeight = ordered.Sum(item => item.shape.Height);
            var gap = (bounds.Bottom - bounds.Top - totalHeight) / (ordered.Length - 1);
            var top = bounds.Top;

            foreach (var item in ordered)
            {
                result[item.index] = item.shape with { Top = top };
                top += item.shape.Height + gap;
            }
        }

        return result;
    }

    private static IReadOnlyList<object> GetShapeItems(dynamic shapeRange)
    {
        var shapes = new List<object>();
        for (int i = 1; i <= shapeRange.Count; i++)
        {
            shapes.Add(shapeRange[i]);
        }

        return shapes;
    }

    private static IReadOnlyList<ShapeBox> GetShapeBoxes(IReadOnlyList<object> shapes)
    {
        var boxes = new List<ShapeBox>();
        foreach (var item in shapes)
        {
            dynamic shape = item;
            boxes.Add(new ShapeBox(
                (float)shape.Left,
                (float)shape.Top,
                (float)shape.Width,
                (float)shape.Height));
        }

        return boxes;
    }

    private static void ApplyShapeBoxes(IReadOnlyList<object> shapes, IReadOnlyList<ShapeBox> boxes)
    {
        for (int i = 0; i < shapes.Count; i++)
        {
            dynamic shape = shapes[i];
            shape.Left = boxes[i].Left;
            shape.Top = boxes[i].Top;
        }
    }

    private static ShapeRangeResult GetShapeRange(dynamic pptApp, int minimumCount)
    {
        dynamic selection = pptApp.ActiveWindow.Selection;
        if (selection.Type != PpSelectionShapes)
            return new ShapeRangeResult(null, $"请先选中至少 {minimumCount} 个形状。");

        dynamic shapes = selection.ShapeRange;
        if (shapes.Count < minimumCount)
            return new ShapeRangeResult(null, $"请先选中至少 {minimumCount} 个形状。");

        return new ShapeRangeResult(shapes, null);
    }

    private sealed record ShapeRangeResult(object? Shapes, string? Error);

    public enum ShapeAlignment
    {
        Left,
        Center,
        Right,
        Top,
        Middle,
        Bottom
    }

    public enum DistributionAxis
    {
        Horizontal,
        Vertical
    }

    public sealed record ShapeBox(float Left, float Top, float Width, float Height)
    {
        public float Right => Left + Width;
        public float Bottom => Top + Height;
    }

    private sealed record ShapeBounds(float Left, float Top, float Right, float Bottom)
    {
        public float CenterX => (Left + Right) / 2f;
        public float CenterY => (Top + Bottom) / 2f;

        public static ShapeBounds From(IReadOnlyList<ShapeBox> shapes)
        {
            float left = shapes.Min(shape => shape.Left);
            float top = shapes.Min(shape => shape.Top);
            float right = shapes.Max(shape => shape.Right);
            float bottom = shapes.Max(shape => shape.Bottom);

            return new ShapeBounds(left, top, right, bottom);
        }
    }
}
