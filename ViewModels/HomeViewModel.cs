using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SplitBillApp.Data;
using SplitBillApp.Services;

namespace SplitBillApp.ViewModels;

public class BillCardVM
{
    public int BillId { get; set; }
    public string Name { get; set; } = "";
    public string PaidByLabel { get; set; } = "";
    public string TotalLabel { get; set; } = "";
    public bool HasCategory { get; set; }
    public string CategoryName { get; set; } = "";
    public string CountLabel { get; set; } = "";
    public List<AvatarVM> Avatars { get; set; } = new();
    public ICommand? OpenCommand { get; set; }
    public ICommand? DeleteCommand { get; set; }
}

public class CategoryCardVM
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = "";
    public string CountLabel { get; set; } = "";
    public string TotalLabel { get; set; } = "";
    public ICommand? OpenCommand { get; set; }
}

public partial class HomeViewModel : BaseViewModel
{
    private readonly DatabaseService _db;
    private readonly DraftService _drafts;

    public ObservableCollection<BillCardVM> BillCards { get; } = new();
    public ObservableCollection<CategoryCardVM> CategoryCards { get; } = new();

    [ObservableProperty] private bool isBillsTab = true;
    public bool IsCatsTab => !IsBillsTab;

    // Tab pill colours (recomputed whenever the tab changes)
    public Color BillsTabBg => IsBillsTab ? Accent : Colors.Transparent;
    public Color BillsTabText => IsBillsTab ? Colors.White : Sub;
    public Color CatsTabBg => IsCatsTab ? Accent : Colors.Transparent;
    public Color CatsTabText => IsCatsTab ? Colors.White : Sub;

    private static Color Accent => Color.FromArgb("#1F8A5B");
    private static Color Sub => Color.FromArgb("#6A756F");

    public HomeViewModel(DatabaseService db, DraftService drafts)
    {
        _db = db;
        _drafts = drafts;
    }

    partial void OnIsBillsTabChanged(bool value)
    {
        OnPropertyChanged(nameof(IsCatsTab));
        OnPropertyChanged(nameof(BillsTabBg));
        OnPropertyChanged(nameof(BillsTabText));
        OnPropertyChanged(nameof(CatsTabBg));
        OnPropertyChanged(nameof(CatsTabText));
    }

    [RelayCommand] private void ShowBills() => IsBillsTab = true;
    [RelayCommand] private void ShowCategories() => IsBillsTab = false;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;

            BillCards.Clear();
            foreach (var b in await _db.GetBillsAsync())
            {
                var avatars = b.People.Take(4).Select((p, i) =>
                    AvatarVM.For(p.Name, p.ColorIndex, size: 30,
                        marginLeft: i == 0 ? 0 : -8, borderWidth: 2)).ToList();

                BillCards.Add(new BillCardVM
                {
                    BillId = b.Id,
                    Name = b.Name,
                    PaidByLabel = $"Paid by 👑 {b.PayerName}",
                    TotalLabel = BillMath.Money(b.Total),
                    HasCategory = !string.IsNullOrEmpty(b.CategoryName),
                    CategoryName = b.CategoryName,
                    CountLabel = $"{b.People.Count} people",
                    Avatars = avatars,
                    OpenCommand = new AsyncRelayCommand(() => OpenBillAsync(b.Id)),
                    DeleteCommand = new AsyncRelayCommand(() => DeleteBillAsync(b.Id, b.Name)),
                });
            }

            CategoryCards.Clear();
            foreach (var c in await _db.GetCategoriesAsync())
            {
                CategoryCards.Add(new CategoryCardVM
                {
                    CategoryId = c.Id,
                    Name = c.Name,
                    CountLabel = $"{c.BillCount} bill{(c.BillCount == 1 ? "" : "s")}",
                    TotalLabel = BillMath.Money(c.Total),
                    OpenCommand = new AsyncRelayCommand(() => OpenCategoryAsync(c.Id)),
                });
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task NewBillAsync()
    {
        _drafts.Start();
        await Shell.Current.GoToAsync("CreateBillPage");
    }

    private static Task OpenBillAsync(int billId)
        => Shell.Current.GoToAsync($"ResultPage?billId={billId}");

    private async Task DeleteBillAsync(int billId, string name)
    {
        var confirmed = await Shell.Current.DisplayAlertAsync("Delete bill?",
            $"\"{name}\" will be removed permanently.", "Delete", "Cancel");
        if (!confirmed) return;

        await _db.DeleteBillAsync(billId);
        await LoadAsync();
    }

    private static Task OpenCategoryAsync(int categoryId)
        => Shell.Current.GoToAsync($"CategoryPage?categoryId={categoryId}");
}
