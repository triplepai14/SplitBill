#if ANDROID
using Android.Views;
using AView = Android.Views.View;

namespace SplitBillApp.Behaviors;

/// <summary>
/// Android's long-press (and therefore MAUI's DragGestureRecognizer) waits for
/// the system "touch and hold" delay — often 500ms or more. This behavior
/// triggers the same long-click after a much shorter hold, so dragging a card
/// feels immediate. Movement or lifting before the delay cancels it, leaving
/// taps and scrolling untouched.
/// </summary>
public class QuickDragBehavior : PlatformBehavior<Microsoft.Maui.Controls.View, AView>
{
    const int HoldMs = 150;     // hold this long before the drag begins
    const float SlopPx = 24f;   // moving further than this first = a scroll

    Android.OS.Handler? _handler;
    Java.Lang.IRunnable? _pending;
    float _downX, _downY;

    protected override void OnAttachedTo(Microsoft.Maui.Controls.View bindable, AView platformView)
    {
        _handler = new Android.OS.Handler(Android.OS.Looper.MainLooper!);
        platformView.Touch += OnTouch;
    }

    protected override void OnDetachedFrom(Microsoft.Maui.Controls.View bindable, AView platformView)
    {
        platformView.Touch -= OnTouch;
        Cancel();
        _handler = null;
    }

    void OnTouch(object? sender, AView.TouchEventArgs e)
    {
        e.Handled = false;   // never swallow the gesture — taps/scroll still work
        if (sender is not AView view || e.Event is null) return;

        switch (e.Event.ActionMasked)
        {
            case MotionEventActions.Down:
                _downX = e.Event.RawX;
                _downY = e.Event.RawY;
                Cancel();
                _pending = new Java.Lang.Runnable(() => view.PerformLongClick());
                _handler?.PostDelayed(_pending, HoldMs);
                break;

            case MotionEventActions.Move:
                if (Math.Abs(e.Event.RawX - _downX) > SlopPx ||
                    Math.Abs(e.Event.RawY - _downY) > SlopPx)
                    Cancel();
                break;

            case MotionEventActions.Up:
            case MotionEventActions.Cancel:
                Cancel();
                break;
        }
    }

    void Cancel()
    {
        if (_pending is null) return;
        _handler?.RemoveCallbacks(_pending);
        _pending = null;
    }
}
#else
namespace SplitBillApp.Behaviors;

// No-op on platforms that don't need the long-press shortcut.
public class QuickDragBehavior : Behavior<Microsoft.Maui.Controls.View> { }
#endif
