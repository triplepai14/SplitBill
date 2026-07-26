using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;

namespace SplitBillAppNew;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    // Tap anywhere outside a text field to dismiss the keyboard (app-wide,
    // without interfering with scrolling or other gestures).
    public override bool DispatchTouchEvent(MotionEvent? e)
    {
        if (e is not null && e.Action == MotionEventActions.Down &&
            CurrentFocus is EditText edit)
        {
            var bounds = new Android.Graphics.Rect();
            edit.GetGlobalVisibleRect(bounds);
            if (!bounds.Contains((int)e.RawX, (int)e.RawY))
            {
                edit.ClearFocus();
                var imm = (InputMethodManager?)GetSystemService(InputMethodService);
                imm?.HideSoftInputFromWindow(edit.WindowToken, HideSoftInputFlags.None);
            }
        }
        return base.DispatchTouchEvent(e);
    }
}
