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

    // ═══════════════════════════════════════════════════════════════
    //  页码与布局
    // ═══════════════════════════════════════════════════════════════

    /// <summary>为所有幻灯片添加页码（页脚右侧）。</summary>
    public static string AddSlideNumbers(dynamic pptApp)
    {
        dynamic presentation = pptApp.ActivePresentation;
        int count = 0;

        foreach (dynamic slide in presentation.Slides)
        {
            try
            {
                // 检查是否已有页码
                bool hasNumber = false;
                foreach (dynamic shape in slide.Shapes)
                {
                    try
                    {
                        if (shape.HasTextFrame != 0 && shape.TextFrame.TextRange.Text.Trim() == slide.SlideIndex.ToString())
                        {
                            hasNumber = true;
                            break;
                        }
                    }
                    catch { }
                }

                if (!hasNumber)
                {
                    float slideWidth = presentation.PageSetup.SlideWidth;
                    float slideHeight = presentation.PageSetup.SlideHeight;
                    dynamic textBox = slide.Shapes.AddTextbox(1, slideWidth - 80, slideHeight - 40, 60, 24); // msoTextOrientationHorizontal
                    textBox.TextFrame.TextRange.Text = slide.SlideIndex.ToString();
                    textBox.TextFrame.TextRange.Font.Size = 10;
                    textBox.TextFrame.TextRange.Font.Color.RGB = 0x666666;
                    textBox.TextFrame.TextRange.ParagraphFormat.Alignment = 2; // ppAlignRight
                    count++;
                }
            }
            catch { }
        }

        return count == 0
            ? "所有幻灯片已有页码。"
            : $"已为 {count} 张幻灯片添加页码。";
    }

    /// <summary>移除所有幻灯片的页码。</summary>
    public static string RemoveSlideNumbers(dynamic pptApp)
    {
        dynamic presentation = pptApp.ActivePresentation;
        int count = 0;

        foreach (dynamic slide in presentation.Slides)
        {
            var toDelete = new List<dynamic>();
            foreach (dynamic shape in slide.Shapes)
            {
                try
                {
                    if (shape.HasTextFrame != 0)
                    {
                        string text = shape.TextFrame.TextRange.Text.Trim();
                        if (text == slide.SlideIndex.ToString() && shape.Width < 80 && shape.Height < 40)
                        {
                            toDelete.Add(shape);
                        }
                    }
                }
                catch { }
            }
            foreach (dynamic shape in toDelete)
            {
                try { shape.Delete(); count++; } catch { }
            }
        }

        return $"已从 {count} 个幻灯片移除页码。";
    }

    // ═══════════════════════════════════════════════════════════════
    //  旋转与交换
    // ═══════════════════════════════════════════════════════════════

    /// <summary>顺时针旋转选中形状 90°。</summary>
    public static string RotateClockwise(dynamic pptApp)
    {
        var result = GetShapeRange(pptApp, minimumCount: 1);
        if (result.Error != null) return result.Error;
        dynamic shapes = result.Shapes;

        for (int i = 1; i <= shapes.Count; i++)
            shapes[i].Rotation += 90f;

        return $"已将 {shapes.Count} 个形状顺时针旋转 90°。";
    }

    /// <summary>逆时针旋转选中形状 90°。</summary>
    public static string RotateCounterClockwise(dynamic pptApp)
    {
        var result = GetShapeRange(pptApp, minimumCount: 1);
        if (result.Error != null) return result.Error;
        dynamic shapes = result.Shapes;

        for (int i = 1; i <= shapes.Count; i++)
            shapes[i].Rotation -= 90f;

        return $"已将 {shapes.Count} 个形状逆时针旋转 90°。";
    }

    /// <summary>交换两个选中形状的位置。</summary>
    public static string SwapPositions(dynamic pptApp)
    {
        var result = GetShapeRange(pptApp, minimumCount: 2);
        if (result.Error != null) return result.Error;
        dynamic shapes = result.Shapes;

        if (shapes.Count != 2)
            return "请恰好选中 2 个形状来交换位置。当前选中了 " + shapes.Count + " 个。";

        float left1 = shapes[1].Left, top1 = shapes[1].Top;
        float width1 = shapes[1].Width, height1 = shapes[1].Height;

        shapes[1].Left = shapes[2].Left;
        shapes[1].Top = shapes[2].Top;
        shapes[1].Width = shapes[2].Width;
        shapes[1].Height = shapes[2].Height;

        shapes[2].Left = left1;
        shapes[2].Top = top1;
        shapes[2].Width = width1;
        shapes[2].Height = height1;

        return $"已交换 '{shapes[1].Name}' 和 '{shapes[2].Name}' 的位置。";
    }

    // ═══════════════════════════════════════════════════════════════
    //  TurboShapes
    // ═══════════════════════════════════════════════════════════════

    /// <summary>插入 Harvey Ball（0-4 象限圆）。</summary>
    public static string InsertHarveyBall(dynamic pptApp, int value = 2)
    {
        value = Math.Clamp(value, 0, 4);
        dynamic slide = pptApp.ActiveWindow.View.Slide;
        float size = 24f;
        float left = 50f;
        float top = 100f;

        // 外圆（边框无填充）
        dynamic outerCircle = slide.Shapes.AddShape(9, left, top, size, size); // msoShapeOval
        outerCircle.Fill.Visible = 0; // msoFalse
        outerCircle.Line.Weight = 1.5f;
        outerCircle.Line.ForeColor.RGB = 0x333333;

        if (value > 0)
        {
            // 内填扇形
            float fillSize = size - 4f;
            float fillLeft = left + 2f;
            float fillTop = top + 2f;
            dynamic pie = slide.Shapes.AddShape(14, fillLeft, fillTop, fillSize, fillSize); // msoShapePie
            pie.Fill.ForeColor.RGB = 0x003366;
            pie.Line.Visible = 0;

            // 调整扇形角度: value 0-4 映射到 90° -> 360° (0.25*PI -> PI)
            // 实际上 msoShapePie 默认 270°, 需要调整为 90°*value
            pie.Adjustments[1] = -90f + (90f * value); // start angle
            pie.Adjustments[2] = -90f; // end angle (-90 = top)
        }

        return $"已插入 Harvey Ball ({value}/4)。";
    }

    /// <summary>插入进度条（0-100%）。</summary>
    public static string InsertProgressBar(dynamic pptApp, int percent = 50)
    {
        percent = Math.Clamp(percent, 0, 100);
        dynamic slide = pptApp.ActiveWindow.View.Slide;
        float barWidth = 200f;
        float barHeight = 16f;
        float left = 50f;
        float top = 100f;

        // 背景矩形
        dynamic bg = slide.Shapes.AddShape(1, left, top, barWidth, barHeight); // msoShapeRectangle
        bg.Fill.ForeColor.RGB = 0xE0E0E0;
        bg.Line.Visible = 0;

        // 填充矩形
        float fillWidth = barWidth * percent / 100f;
        if (fillWidth > 0)
        {
            dynamic fill = slide.Shapes.AddShape(1, left, top, fillWidth, barHeight);
            fill.Fill.ForeColor.RGB = percent switch
            {
                >= 80 => 0x107C10,  // green
                >= 40 => 0x0078D4,  // blue
                _ => 0xD83B01       // red
            };
            fill.Line.Visible = 0;
        }

        return $"已插入进度条 ({percent}%)。";
    }

    /// <summary>插入星级评分（1-5 星）。</summary>
    public static string InsertRatingStars(dynamic pptApp, int stars = 3)
    {
        stars = Math.Clamp(stars, 1, 5);
        dynamic slide = pptApp.ActiveWindow.View.Slide;
        float starSize = 22f;
        float gap = 4f;
        float left = 50f;
        float top = 100f;

        for (int i = 1; i <= 5; i++)
        {
            dynamic star = slide.Shapes.AddShape(92, left + (i - 1) * (starSize + gap), top, starSize, starSize); // msoShape5pointStar
            star.Line.ForeColor.RGB = 0xC0C0C0;
            star.Line.Weight = 0.5f;

            if (i <= stars)
            {
                star.Fill.ForeColor.RGB = 0xFFB900; // gold
            }
            else
            {
                star.Fill.ForeColor.RGB = 0xF0F0F0; // light gray
            }
        }

        return $"已插入 {stars}/5 星评分。";
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
