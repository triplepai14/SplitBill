using SplitBillApp.ViewModels;

namespace SplitBillApp.Views;

public partial class CategoryPage : ContentPage
{
    public CategoryPage(CategoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    // Share the settle-up list as a picture of just that section; if there's
    // nothing to settle (or the capture fails), fall back to the text summary.
    private async void OnShareClicked(object? sender, EventArgs e)
    {
        if (BindingContext is not CategoryViewModel vm) return;

        if (vm.HasSettlements)
        {
            try
            {
                var shot = await SettleUpSection.CaptureAsync();
                if (shot is not null)
                {
                    var path = Path.Combine(FileSystem.CacheDirectory,
                        $"settle-up-{vm.CategoryId}.png");
                    await using (var src = await shot.OpenReadAsync(ScreenshotFormat.Png))
                    await using (var dst = File.Create(path))
                        await src.CopyToAsync(dst);

                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = vm.CatTitle,
                        File = new ShareFile(path),
                    });
                    return;
                }
            }
            catch
            {
                // fall through to the text summary
            }
        }

        await vm.ShareCommand.ExecuteAsync(null);
    }
}
