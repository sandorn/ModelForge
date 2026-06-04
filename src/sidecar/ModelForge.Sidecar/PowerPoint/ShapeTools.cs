namespace ModelForge.Sidecar.PowerPoint;

/// <summary>
/// PPT 形状批量操作工具。对齐、分布、统一尺寸。
/// </summary>
public static class ShapeTools
{
    /// <summary>
    /// 将选中的形状左对齐到第一个形状的位置。
    /// </summary>
    public static string AlignLeft(dynamic pptApp)
    {
        dynamic selection = pptApp.ActiveWindow.Selection;
        if (selection.Type != 2) return "请先选中多个形状。"; // ppSelectionShapes = 2

        dynamic shapeRange = selection.ShapeRange;
        float left = shapeRange[1].Left;

        for (int i = 2; i <= shapeRange.Count; i++)
            shapeRange[i].Left = left;

        return $"已将 {shapeRange.Count} 个形状左对齐。";
    }

    /// <summary>
    /// 将选中的形状在水平方向上均匀分布。
    /// </summary>
    public static string DistributeHorizontal(dynamic pptApp)
    {
        dynamic selection = pptApp.ActiveWindow.Selection;
        if (selection.Type != 2 || selection.ShapeRange.Count < 3)
            return "请先选中至少 3 个形状。";

        dynamic shapes = selection.ShapeRange;
        shapes.Distribute(0, 0); // msoDistributeHorizontally

        return $"已将 {shapes.Count} 个形状水平均分。";
    }

    /// <summary>
    /// 将选中形状统一为相同宽度（以第一个形状为准）。
    /// </summary>
    public static string UnifySize(dynamic pptApp)
    {
        dynamic selection = pptApp.ActiveWindow.Selection;
        if (selection.Type != 2 || selection.ShapeRange.Count < 2)
            return "请先选中至少 2 个形状。";

        dynamic shapes = selection.ShapeRange;
        float width = shapes[1].Width;
        float height = shapes[1].Height;

        for (int i = 2; i <= shapes.Count; i++)
        {
            shapes[i].Width = width;
            shapes[i].Height = height;
        }

        return $"已将 {shapes.Count} 个形状统一为 {width}x{height}。";
    }
}
