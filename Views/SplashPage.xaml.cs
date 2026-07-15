namespace SplitBillApp.Views;

// Branded in-app splash: fades the logo in, holds a beat, then hands the
// window over to the real AppShell.
public partial class SplashPage : ContentPage
{
    private bool _started;

    public SplashPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_started) return;   // guard against re-entry
        _started = true;

        await Task.WhenAll(
            Root.FadeToAsync(1, 550, Easing.CubicOut),
            Root.TranslateToAsync(0, 0, 650, Easing.CubicOut));

        await Task.Delay(1300);
        await Root.FadeToAsync(0, 250, Easing.CubicIn);

        if (Window is not null)
            Window.Page = new AppShell();
    }
}
