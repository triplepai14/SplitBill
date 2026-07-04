using SplitBillApp.ViewModels;

namespace SplitBillApp.Views;

public partial class MenusPage : ContentPage
{
    private readonly MenusViewModel _vm;

    public MenusPage(MenusViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
