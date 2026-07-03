using SplitBillApp.Views;

namespace SplitBillApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Routes navigated to via Shell.Current.GoToAsync that are not
		// declared as ShellContent must be registered explicitly.
		Routing.RegisterRoute(nameof(GroupDetailPage), typeof(GroupDetailPage));
		Routing.RegisterRoute(nameof(AddExpensePage), typeof(AddExpensePage));
	}
}
