using SplitBillApp.ViewModels;

namespace SplitBillApp.Views;

public partial class AddExpensePage : ContentPage
{
    public AddExpensePage(AddExpenseViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
