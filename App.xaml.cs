using SplitBillApp.Views;

namespace SplitBillApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    // Branded splash first; it swaps itself for the AppShell when done.
    protected override Window CreateWindow(IActivationState? activationState)
        => new(new SplashPage());
}
