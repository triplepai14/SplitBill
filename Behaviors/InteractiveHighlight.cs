namespace SplitBillApp.Behaviors;

/// <summary>
/// Attached to tappable cards/rows via a Style setter
/// (behaviors:InteractiveHighlight.IsEnabled="True"). Smoothly animates the
/// background, stroke and scale between three states:
///   normal → hover (pointer over) → pressed (tap held down).
/// Colours fade gradually instead of snapping.
/// </summary>
public static class InteractiveHighlight
{
    // "Garden" theme interaction colours
    static readonly Color NormalBg = Color.FromArgb("#FFFFFF");
    static readonly Color NormalStroke = Color.FromArgb("#E7EAE4");
    static readonly Color HoverBg = Color.FromArgb("#F0F6F1");
    static readonly Color HoverStroke = Color.FromArgb("#BFDECB");
    static readonly Color PressedBg = Color.FromArgb("#DCEBE1");
    static readonly Color PressedStroke = Color.FromArgb("#9CC9AE");

    public static readonly BindableProperty IsEnabledProperty =
        BindableProperty.CreateAttached(
            "IsEnabled", typeof(bool), typeof(InteractiveHighlight), false,
            propertyChanged: OnIsEnabledChanged);

    public static bool GetIsEnabled(BindableObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(BindableObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    static void OnIsEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not Border border || newValue is not true) return;

        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) => Transition(border, HoverBg, HoverStroke, 1.0);
        pointer.PointerExited += (_, _) => Transition(border, NormalBg, NormalStroke, 1.0);
        pointer.PointerPressed += (_, _) => Transition(border, PressedBg, PressedStroke, 0.98);
        pointer.PointerReleased += (_, _) => Transition(border, HoverBg, HoverStroke, 1.0);
        border.GestureRecognizers.Add(pointer);
    }

    // Fade colours and scale toward the target state instead of snapping.
    static void Transition(Border border, Color bg, Color stroke, double scale)
    {
        var bgFrom = border.BackgroundColor ?? NormalBg;
        var strokeFrom = (border.Stroke as SolidColorBrush)?.Color ?? NormalStroke;
        var scaleFrom = border.Scale;

        border.AbortAnimation("ixhl");
        new Animation(t =>
        {
            border.BackgroundColor = Lerp(bgFrom, bg, t);
            border.Stroke = new SolidColorBrush(Lerp(strokeFrom, stroke, t));
            border.Scale = scaleFrom + (scale - scaleFrom) * t;
        }).Commit(border, "ixhl", 16, 220, Easing.CubicOut);
    }

    static Color Lerp(Color a, Color b, double t) => new(
        (float)(a.Red + (b.Red - a.Red) * t),
        (float)(a.Green + (b.Green - a.Green) * t),
        (float)(a.Blue + (b.Blue - a.Blue) * t),
        (float)(a.Alpha + (b.Alpha - a.Alpha) * t));
}
