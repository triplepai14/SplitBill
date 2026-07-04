using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SplitBillApp.Data;
using SplitBillApp.Services;

namespace SplitBillApp.ViewModels;

public class CatBillVM
{
    public string Name { get; set; } = "";
    public string PaidByLabel { get; set; } = "";
    public string TotalLabel { get; set; } = "";
    public ICommand? OpenCommand { get; set; }
}

public class CatPersonVM
{
    public string Name { get; set; } = "";
    public string Initial { get; set; } = "";
    public Color AvatarColor { get; set; } = Colors.Gray;
    public string AmountLabel { get; set; } = "";
}

[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class CategoryViewModel : BaseViewModel
{
    private readonly DatabaseService _db;

    [ObservableProperty] private int categoryId;
    [ObservableProperty] private string catTitle = "";
    [ObservableProperty] private string catTotalLabel = "";
    [ObservableProperty] private string catCountLabel = "";

    public ObservableCollection<CatBillVM> Bills { get; } = new();
    public ObservableCollection<CatPersonVM> People { get; } = new();

    public CategoryViewModel(DatabaseService db) => _db = db;

    partial void OnCategoryIdChanged(int value) => _ = LoadAsync();

    public async Task LoadAsync()
    {
        if (CategoryId == 0) return;

        var cat = await _db.GetCategoryAsync(CategoryId);
        var bills = await _db.GetBillsAsync(CategoryId);
        var total = bills.Sum(b => b.Total);

        CatTitle = cat?.Name ?? "Category";
        CatTotalLabel = BillMath.Money(total);
        CatCountLabel = $"{bills.Count} bill{(bills.Count == 1 ? "" : "s")}";

        Bills.Clear();
        var agg = new Dictionary<int, decimal>();
        var names = new Dictionary<int, (string Name, int Color)>();

        foreach (var b in bills)
        {
            var billId = b.Id;
            Bills.Add(new CatBillVM
            {
                Name = b.Name,
                PaidByLabel = $"Paid by {b.PayerName}",
                TotalLabel = BillMath.Money(b.Total),
                OpenCommand = new AsyncRelayCommand(() =>
                    Shell.Current.GoToAsync($"ResultPage?billId={billId}")),
            });

            var detail = await _db.GetBillDetailAsync(b.Id);
            if (detail is null) continue;
            foreach (var p in detail.People)
            {
                agg[p.Id] = agg.GetValueOrDefault(p.Id, 0m) + detail.Shares.GetValueOrDefault(p.Id, 0m);
                names[p.Id] = (p.Name, p.ColorIndex);
            }
        }

        People.Clear();
        foreach (var kv in agg.OrderByDescending(k => k.Value))
        {
            var (name, color) = names[kv.Key];
            People.Add(new CatPersonVM
            {
                Name = name,
                Initial = BillMath.Initial(name),
                AvatarColor = Color.FromArgb(BillMath.ColorForIndex(color)),
                AmountLabel = BillMath.Money(kv.Value),
            });
        }
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("//HomePage");
}
