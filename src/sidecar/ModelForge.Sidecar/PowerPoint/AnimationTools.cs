namespace ModelForge.Sidecar.PowerPoint;

/// <summary>
/// PPT animation tools — apply basic animation effects to shapes.
/// </summary>
public static class AnimationTools
{
    public static string ApplyAppear(dynamic pptApp)
    {
        dynamic selection = pptApp.ActiveWindow.Selection;
        if (selection.Type != 2) return "Please select a shape first.";

        dynamic shape = selection.ShapeRange[1];
        dynamic slide = pptApp.ActiveWindow.View.Slide;
        dynamic sequence = slide.TimeLine.MainSequence;
        dynamic effect = sequence.AddEffect(shape, 1, 0, 1); // msoAnimEffectAppear

        return $"Applied Appear animation to '{shape.Name}'.";
    }

    public static string ApplyFade(dynamic pptApp)
    {
        dynamic selection = pptApp.ActiveWindow.Selection;
        if (selection.Type != 2) return "Please select a shape first.";

        dynamic shape = selection.ShapeRange[1];
        dynamic slide = pptApp.ActiveWindow.View.Slide;
        dynamic sequence = slide.TimeLine.MainSequence;
        dynamic effect = sequence.AddEffect(shape, 10, 0, 1); // msoAnimEffectFade

        return $"Applied Fade animation to '{shape.Name}'.";
    }

    public static string ApplyFlyIn(dynamic pptApp)
    {
        dynamic selection = pptApp.ActiveWindow.Selection;
        if (selection.Type != 2) return "Please select a shape first.";

        dynamic shape = selection.ShapeRange[1];
        dynamic slide = pptApp.ActiveWindow.View.Slide;
        dynamic sequence = slide.TimeLine.MainSequence;
        dynamic effect = sequence.AddEffect(shape, 64, 0, 1); // msoAnimEffectFly

        try { effect.EffectParameters.Direction = 4; } catch { } // msoAnimDirectionBottom

        return $"Applied Fly-In (bottom) animation to '{shape.Name}'.";
    }

    public static string ClearAnimations(dynamic pptApp)
    {
        dynamic selection = pptApp.ActiveWindow.Selection;
        if (selection.Type != 2) return "Please select a shape first.";

        dynamic shape = selection.ShapeRange[1];
        dynamic slide = pptApp.ActiveWindow.View.Slide;
        int removed = 0;

        for (int i = slide.TimeLine.MainSequence.Count; i >= 1; i--)
        {
            try
            {
                dynamic effect = slide.TimeLine.MainSequence[i];
                if (effect.Shape.Name == shape.Name)
                {
                    effect.Delete();
                    removed++;
                }
            }
            catch { }
        }

        return removed > 0
            ? $"Cleared {removed} animation(s) from '{shape.Name}'."
            : $"Shape '{shape.Name}' has no animations.";
    }
}
