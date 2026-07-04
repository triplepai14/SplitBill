using SplitBillApp.ViewModels;

namespace SplitBillApp.Views;

public partial class ResultPage : ContentPage
{
    public ResultPage(ResultViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
