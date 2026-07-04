using SplitBillApp.ViewModels;

namespace SplitBillApp.Views;

public partial class CategoryPage : ContentPage
{
    public CategoryPage(CategoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
