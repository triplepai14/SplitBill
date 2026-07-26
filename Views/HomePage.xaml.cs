using SplitBillApp.ViewModels;

namespace SplitBillApp.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _vm;

    public HomePage(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    // Stash the dragged bill so the category's Drop handler can read it.
    private void OnBillDragStarting(object? sender, DragStartingEventArgs e)
    {
        if (sender is Element el && el.BindingContext is BillRowVM bill)
            e.Data.Properties["bill"] = bill;
    }

    // Dropped a bill onto a group header → move it into that category (0 = out).
    private async void OnCategoryDrop(object? sender, DropEventArgs e)
    {
        if (sender is Element el && el.BindingContext is CategoryGroupVM group)
        {
            group.IsDragOver = false;
            if (e.Data.Properties.TryGetValue("bill", out var payload) && payload is BillRowVM bill)
                await _vm.MoveBillAsync(bill, group.CategoryId);
        }
    }

    // Highlight the header while a bill is dragged over it.
    private void OnGroupDragOver(object? sender, DragEventArgs e)
    {
        if (sender is Element el && el.BindingContext is CategoryGroupVM group)
            group.IsDragOver = true;
    }

    private void OnGroupDragLeave(object? sender, DragEventArgs e)
    {
        if (sender is Element el && el.BindingContext is CategoryGroupVM group)
            group.IsDragOver = false;
    }
}
