using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AView = Android.Views.View;

namespace SplitBillAppNew;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    // Android's own long-press wait (500ms+, longer with accessibility
    // settings) makes dragging a card feel sluggish. We watch touches here —
    // rather than attaching a touch listener, which would swallow MAUI's tap
    // gestures — and fire the long-click sooner.
    const int QuickDragHoldMs = 125;
    const float SlopPx = 30f;

    Handler? _dragHandler;
    Java.Lang.IRunnable? _pendingDrag;
    float _downX, _downY;

    public override bool DispatchTouchEvent(MotionEvent? e)
    {
        DismissKeyboardOnOutsideTap(e);
        ScheduleQuickDrag(e);
        return base.DispatchTouchEvent(e);
    }

    // Tap anywhere outside a text field to dismiss the keyboard.
    void DismissKeyboardOnOutsideTap(MotionEvent? e)
    {
        if (e is null || e.Action != MotionEventActions.Down || CurrentFocus is not EditText edit)
            return;

        var bounds = new Android.Graphics.Rect();
        edit.GetGlobalVisibleRect(bounds);
        if (bounds.Contains((int)e.RawX, (int)e.RawY)) return;

        edit.ClearFocus();
        var imm = (InputMethodManager?)GetSystemService(InputMethodService);
        imm?.HideSoftInputFromWindow(edit.WindowToken, HideSoftInputFlags.None);
    }

    void ScheduleQuickDrag(MotionEvent? e)
    {
        if (e is null) return;

        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
                CancelQuickDrag();
                _downX = e.RawX;
                _downY = e.RawY;

                var target = FindDraggable(Window?.DecorView, (int)e.RawX, (int)e.RawY);
                if (target is null) return;

                _dragHandler ??= new Handler(Looper.MainLooper!);
                _pendingDrag = new Java.Lang.Runnable(() => target.PerformLongClick());
                _dragHandler.PostDelayed(_pendingDrag, QuickDragHoldMs);
                break;

            case MotionEventActions.Move:
                // Moving first means the user is scrolling, not picking up a card.
                if (Math.Abs(e.RawX - _downX) > SlopPx || Math.Abs(e.RawY - _downY) > SlopPx)
                    CancelQuickDrag();
                break;

            case MotionEventActions.Up:
            case MotionEventActions.Cancel:
                CancelQuickDrag();
                break;
        }
    }

    void CancelQuickDrag()
    {
        if (_pendingDrag is null) return;
        _dragHandler?.RemoveCallbacks(_pendingDrag);
        _pendingDrag = null;
    }

    // Deepest long-clickable view under the finger — that's the draggable card.
    // Text fields are skipped so their selection handles keep working.
    static AView? FindDraggable(AView? view, int x, int y)
    {
        if (view is null || view.Visibility != ViewStates.Visible) return null;

        var loc = new int[2];
        view.GetLocationOnScreen(loc);
        if (x < loc[0] || x > loc[0] + view.Width || y < loc[1] || y > loc[1] + view.Height)
            return null;

        if (view is ViewGroup group)
        {
            for (var i = group.ChildCount - 1; i >= 0; i--)
            {
                var hit = FindDraggable(group.GetChildAt(i), x, y);
                if (hit is not null) return hit;
            }
        }

        return view.LongClickable && view is not EditText ? view : null;
    }
}
