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
    public override bool DispatchTouchEvent(MotionEvent? e)
    {
        DismissKeyboardOnOutsideTap(e);
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
}
