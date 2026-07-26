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

    // Tapping empty space unfocuses any entry, which dismisses the keyboard.
    private void OnDismissKeyboard(object? sender, TappedEventArgs e)
    {
        foreach (var v in this.GetVisualTreeDescendants().OfType<VisualElement>())
            if (v is Entry or Editor && v.IsFocused)
                v.Unfocus();
    }
}
