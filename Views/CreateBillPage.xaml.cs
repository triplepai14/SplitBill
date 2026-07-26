using SplitBillApp.ViewModels;

namespace SplitBillApp.Views;

public partial class CreateBillPage : ContentPage
{
    private readonly CreateBillViewModel _vm;

    public CreateBillPage(CreateBillViewModel vm)
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
