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

    private Border? _draggingBorder;

    // Stash the dragged bill and lift + tint its card so the long-press reads.
    private void OnBillDragStarting(object? sender, DragStartingEventArgs e)
    {
        if (sender is Element el && el.BindingContext is BillRowVM bill)
            e.Data.Properties["bill"] = bill;

        if (sender is GestureRecognizer g && g.Parent is Border b)
        {
            _draggingBorder = b;
            b.BackgroundColor = Color.FromArgb("#CDE7D6");   // soft green highlight
            _ = b.ScaleTo(1.03, 120, Easing.CubicOut);
        }
    }

    // Settle the card back after the drag ends.
    private void OnBillDropCompleted(object? sender, DropCompletedEventArgs e)
    {
        if (_draggingBorder is Border b)
        {
            b.BackgroundColor = Colors.White;
            _ = b.ScaleTo(1.0, 120, Easing.CubicIn);
            _draggingBorder = null;
        }
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
