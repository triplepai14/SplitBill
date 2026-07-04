using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SplitBillApp.Data;
using SplitBillApp.Models;
using SplitBillApp.Services;

namespace SplitBillApp.ViewModels;

// One editable line in the itemize list, with a per-person exclusion row.
public partial class MenuRowVM : ObservableObject
{
    private readonly DraftItem _item;
    private readonly List<Person> _people;

    public string Name => _item.Name;
    public string PriceLabel => BillMath.Money(_item.Price);
    [ObservableProperty] private string splitLabel = "";

    public ObservableCollection<ToggleChip> ExcludeChips { get; } = new();
    public ICommand RemoveCommand { get; }

    public MenuRowVM(DraftItem item, List<Person> people, Action<MenuRowVM> onRemove)
    {
        _item = item;
        _people = people;
        RemoveCommand = new RelayCommand(() => onRemove(this));

        foreach (var p in people)
        {
            var chip = new ToggleChip
            {
                Id = p.Id, Name = p.Name, HasAvatar = true, Variant = ChipVariant.Exclude,
                Initial = BillMath.Initial(p.Name),
                AvatarColor = Color.FromArgb(BillMath.ColorForIndex(p.ColorIndex)),
                Active = !_item.Excluded.Contains(p.Id),   // Active == included
            };
            chip.ToggleCommand = new RelayCommand(() => ToggleExclude(chip));
            ExcludeChips.Add(chip);
        }
        UpdateSplitLabel();
    }

    public DraftItem Item => _item;

    private void ToggleExclude(ToggleChip chip)
    {
        if (_item.Excluded.Contains(chip.Id)) _item.Excluded.Remove(chip.Id);
        else _item.Excluded.Add(chip.Id);
        chip.Active = !_item.Excluded.Contains(chip.Id);
        UpdateSplitLabel();
    }

    private void UpdateSplitLabel()
    {
        var n = _people.Count(p => !_item.Excluded.Contains(p.Id));
        SplitLabel = $"Split {n} way{(n == 1 ? "" : "s")}";
    }
}

public partial class MenusViewModel : BaseViewModel
{
    private readonly DatabaseService _db;
    private readonly DraftService _drafts;
    private List<Person> _people = new();

    private BillDraft Draft => _drafts.Current ?? _drafts.Start();

    [ObservableProperty] private string flatTotal = string.Empty;
    [ObservableProperty] private string itemName = string.Empty;
    [ObservableProperty] private string itemPrice = string.Empty;
    [ObservableProperty] private bool isFlat = true;

    public bool IsItems => !IsFlat;
    public string MenusTitle => IsFlat ? "Bill total" : "Add items";
    public string RunningLabel => BillMath.Money(Draft.RunningTotal);
    public string FlatEachLabel
    {
        get
        {
            var n = Draft.PeopleIds.Count;
            if (n == 0) return "Add people to split between";
            var amt = decimal.TryParse(FlatTotal, out var t) ? t : 0m;
            return $"{BillMath.Money(amt / n)} each · split between {n} {(n == 1 ? "person" : "people")}";
        }
    }

    public Color FlatBg => IsFlat ? Accent : Colors.Transparent;
    public Color FlatText => IsFlat ? Colors.White : Sub;
    public Color ItemsBg => IsItems ? Accent : Colors.Transparent;
    public Color ItemsText => IsItems ? Colors.White : Sub;
    private static Color Accent => Color.FromArgb("#1F8A5B");
    private static Color Sub => Color.FromArgb("#6A756F");

    public ObservableCollection<MenuRowVM> Items { get; } = new();

    public MenusViewModel(DatabaseService db, DraftService drafts)
    {
        _db = db;
        _drafts = drafts;
    }

    public async Task LoadAsync()
    {
        var all = await _db.GetPeopleAsync();
        _people = all.Where(p => Draft.PeopleIds.Contains(p.Id)).ToList();

        IsFlat = Draft.Flat;
        FlatTotal = Draft.FlatTotal;

        Items.Clear();
        foreach (var it in Draft.Items) Items.Add(NewRow(it));

        RaiseTotals();
    }

    private MenuRowVM NewRow(DraftItem it) => new(it, _people, RemoveRow);

    partial void OnIsFlatChanged(bool value)
    {
        Draft.Flat = value;
        OnPropertyChanged(nameof(IsItems));
        OnPropertyChanged(nameof(MenusTitle));
        OnPropertyChanged(nameof(FlatBg));
        OnPropertyChanged(nameof(FlatText));
        OnPropertyChanged(nameof(ItemsBg));
        OnPropertyChanged(nameof(ItemsText));
        RaiseTotals();
    }

    partial void OnFlatTotalChanged(string value)
    {
        Draft.FlatTotal = value;
        RaiseTotals();
    }

    private void RaiseTotals()
    {
        OnPropertyChanged(nameof(RunningLabel));
        OnPropertyChanged(nameof(FlatEachLabel));
    }

    [RelayCommand] private void ModeFlat() => IsFlat = true;
    [RelayCommand] private void ModeItems() => IsFlat = false;

    [RelayCommand]
    private void AddItem()
    {
        var name = (ItemName ?? "").Trim();
        if (name.Length == 0) return;
        if (!decimal.TryParse(ItemPrice, out var price) || price <= 0) return;

        var item = new DraftItem { Name = name, Price = price };
        Draft.Items.Add(item);
        Items.Add(NewRow(item));

        ItemName = string.Empty;
        ItemPrice = string.Empty;
        RaiseTotals();
    }

    private void RemoveRow(MenuRowVM row)
    {
        Draft.Items.Remove(row.Item);
        Items.Remove(row);
        RaiseTotals();
    }

    [RelayCommand]
    private async Task FinishAsync()
    {
        var d = Draft;
        d.PayerId ??= d.PeopleIds.FirstOrDefault();

        List<(string, decimal, List<int>)> items;
        if (d.Flat)
        {
            var total = decimal.TryParse(d.FlatTotal, out var t) ? t : 0m;
            items = new() { ("Total", total, new List<int>()) };
        }
        else
        {
            items = d.Items.Select(i => (i.Name, i.Price, i.Excluded.ToList())).ToList();
        }

        var bill = new Bill
        {
            Name = string.IsNullOrWhiteSpace(d.Name) ? "Untitled bill" : d.Name.Trim(),
            CategoryId = d.CategoryId ?? 0,
            PayerId = d.PayerId ?? 0,
            IsFlat = d.Flat,
            CreatedDate = DateTime.Now,
        };

        var id = await _db.SaveNewBillAsync(bill, d.PeopleIds, items);
        _drafts.Clear();
        await Shell.Current.GoToAsync($"ResultPage?billId={id}&created=1");
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}
