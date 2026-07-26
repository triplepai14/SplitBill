using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SplitBillApp.Data;
using SplitBillApp.Models;
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
    private CategoryStats? _stats;
    private Category? _category;
    private bool _suppressDateSave;

    [ObservableProperty] private int categoryId;
    [ObservableProperty] private string catTitle = "";
    [ObservableProperty] private string catTotalLabel = "";
    [ObservableProperty] private string catCountLabel = "";
    [ObservableProperty] private bool hasSettlements;
    [ObservableProperty] private DateTime catDate = DateTime.Now;
    [ObservableProperty] private bool isDone;
    public string DoneLabel => IsDone ? "↩ Reopen trip" : "✓ Mark trip done";
    partial void OnIsDoneChanged(bool value) => OnPropertyChanged(nameof(DoneLabel));

    public ObservableCollection<CatBillVM> Bills { get; } = new();
    public ObservableCollection<CatPersonVM> People { get; } = new();
    public ObservableCollection<SettlementVM> Settlements { get; } = new();

    public CategoryViewModel(DatabaseService db) => _db = db;

    partial void OnCategoryIdChanged(int value) => _ = LoadAsync();

    public async Task LoadAsync()
    {
        if (CategoryId == 0) return;

        var s = await _db.GetCategoryStatsAsync(CategoryId);
        if (s is null) return;
        _stats = s;
        _category = s.Category;

        CatTitle = s.Category.Name;
        CatTotalLabel = BillMath.Money(s.Total);
        CatCountLabel = $"{s.Bills.Count} bill{(s.Bills.Count == 1 ? "" : "s")} · {s.People.Count} people";
        IsDone = s.Category.IsDone;
        _suppressDateSave = true;      // assigning CatDate shouldn't trigger a save
        CatDate = s.Category.Date;
        _suppressDateSave = false;

        Bills.Clear();
        foreach (var b in s.Bills)
        {
            var billId = b.Id;
            Bills.Add(new CatBillVM
            {
                Name = b.Name,
                PaidByLabel = $"Paid by 👑 {b.PayerName}",
                TotalLabel = BillMath.Money(b.Total),
                OpenCommand = new AsyncRelayCommand(() =>
                    Shell.Current.GoToAsync($"ResultPage?billId={billId}")),
            });
        }

        People.Clear();
        foreach (var p in s.People.OrderByDescending(p => s.Spent.GetValueOrDefault(p.Id, 0m)))
        {
            People.Add(new CatPersonVM
            {
                Name = p.Name,
                Initial = BillMath.Initial(p.Name),
                AvatarColor = Color.FromArgb(BillMath.ColorForIndex(p.ColorIndex)),
                AmountLabel = BillMath.Money(s.Spent.GetValueOrDefault(p.Id, 0m)),
            });
        }

        Settlements.Clear();
        foreach (var (from, to, amount) in s.Settlements)
        {
            Settlements.Add(new SettlementVM
            {
                FromName = from.Name,
                FromInitial = BillMath.Initial(from.Name),
                FromColor = Color.FromArgb(BillMath.ColorForIndex(from.ColorIndex)),
                ToName = to.Name,
                ToInitial = BillMath.Initial(to.Name),
                ToColor = Color.FromArgb(BillMath.ColorForIndex(to.ColorIndex)),
                AmountLabel = BillMath.Money(amount),
            });
        }
        HasSettlements = Settlements.Count > 0;
    }

    partial void OnCatDateChanged(DateTime value)
    {
        if (_suppressDateSave || _category is null) return;
        _category.Date = value;
        _ = _db.UpdateCategoryAsync(_category);
    }

    [RelayCommand]
    private async Task ToggleDoneAsync()
    {
        if (_category is null) return;
        await _db.SetCategoryDoneAsync(_category.Id, !_category.IsDone);
        IsDone = !IsDone;
        _category.IsDone = IsDone;
    }

    [RelayCommand]
    private async Task RenameAsync()
    {
        if (_category is null) return;
        var name = await Shell.Current.DisplayPromptAsync(
            "Rename category", "Name", "Save", "Cancel", initialValue: _category.Name);
        if (string.IsNullOrWhiteSpace(name)) return;
        _category.Name = name.Trim();
        await _db.UpdateCategoryAsync(_category);
        CatTitle = _category.Name;
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("//HomePage");

    [RelayCommand]
    private async Task ShareAsync()
    {
        if (_stats is null) return;
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = CatTitle,
            Text = SummaryText.ForCategory(_stats),
        });
    }

    // Permanently remove this category and every bill inside it.
    [RelayCommand]
    private async Task DeleteAsync()
    {
        var confirmed = await Shell.Current.DisplayAlertAsync("Delete category?",
            $"\"{CatTitle}\" and all {Bills.Count} bill{(Bills.Count == 1 ? "" : "s")} in it will be removed permanently.",
            "Delete", "Cancel");
        if (!confirmed) return;

        await _db.DeleteCategoryAsync(CategoryId);
        await Shell.Current.GoToAsync("//HomePage");
    }
}
