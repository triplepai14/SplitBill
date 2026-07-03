using SplitBillApp.ViewModels;

namespace SplitBillApp.Views;

public partial class GroupDetailPage : ContentPage
{
    private readonly GroupDetailViewModel _vm;

    public GroupDetailPage(GroupDetailViewModel vm)
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
