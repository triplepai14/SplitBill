using SplitBillApp.Views;

namespace SplitBillApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Routes navigated to via Shell.Current.GoToAsync that are not
		// declared as ShellContent must be registered explicitly.
		Routing.RegisterRoute(nameof(CreateBillPage), typeof(CreateBillPage));
		Routing.RegisterRoute(nameof(MenusPage), typeof(MenusPage));
		Routing.RegisterRoute(nameof(ResultPage), typeof(ResultPage));
		Routing.RegisterRoute(nameof(CategoryPage), typeof(CategoryPage));
	}
}
