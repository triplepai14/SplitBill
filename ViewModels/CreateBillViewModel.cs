using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SplitBillApp.Data;
using SplitBillApp.Models;
using SplitBillApp.Services;

namespace SplitBillApp.ViewModels;

// One editable line in the items list, with a per-person exclusion row.
public partial class MenuRowVM : ObservableObject
{
    private readonly DraftItem _item;
    private readonly List<Person> _people;

    public string Name => _item.Name;
    public string PriceLabel => BillMath.Money(_item.Price);
    [ObservableProperty] private string splitLabel = "";

    public ObservableCollection<ToggleChip> ExcludeChips { get; } = new();
    public ICommand RemoveCommand { get; }

    public MenuRowVM(DraftItem item, List<Person> people, int? payerId, Action<MenuRowVM> onRemove)
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
                IsPayer = p.Id == payerId,                 // crown the payer
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

/// <summary>
/// The single create/edit page: bill details (name, category, people, payer)
/// followed by the total amount and the not-shared-equally items, all on one
/// screen. Finishing saves the bill and opens the result.
/// </summary>
public partial class CreateBillViewModel : BaseViewModel
{
    private readonly DatabaseService _db;
    private readonly DraftService _drafts;
    private List<Person> _allPeople = new();

    private BillDraft Draft => _drafts.Current ?? _drafts.Start();

    // ---- bill details ----
    [ObservableProperty] private string billName = string.Empty;
    [ObservableProperty] private string newCategoryName = string.Empty;
    [ObservableProperty] private string newPersonName = string.Empty;
    [ObservableProperty] private bool hasPeople;

    // "New bill" when creating, "Edit bill" when reopening an existing one.
    [ObservableProperty] private string pageTitle = "New bill";

    // Editable bill date/time (defaults to now).
    [ObservableProperty] private DateTime billDate = DateTime.Now;
    [ObservableProperty] private TimeSpan billTime = DateTime.Now.TimeOfDay;

    // Footer button: creating shows the result next, editing just saves.
    [ObservableProperty] private string finishLabel = "See who owes what";

    // The add-new-category / add-new-person inputs stay hidden until the
    // little + button next to the section header is tapped.
    [ObservableProperty] private bool showAddCategory;
    [ObservableProperty] private bool showAddPerson;

    public string AddCategoryIcon => ShowAddCategory ? "✕" : "+";
    public string AddPersonIcon => ShowAddPerson ? "✕" : "+";

    partial void OnShowAddCategoryChanged(bool value) => OnPropertyChanged(nameof(AddCategoryIcon));
    partial void OnShowAddPersonChanged(bool value) => OnPropertyChanged(nameof(AddPersonIcon));

    [RelayCommand] private void ToggleAddCategory() => ShowAddCategory = !ShowAddCategory;
    [RelayCommand] private void ToggleAddPerson() => ShowAddPerson = !ShowAddPerson;

    // Collapse the category/people/payer editors into a one-line summary until
    // the user taps + to edit (expanded by default only for a brand-new bill).
    [ObservableProperty] private bool showDetails;
    public bool DetailsCollapsed => !ShowDetails;
    public string DetailsIcon => ShowDetails ? "✕" : "✎";   // pencil = edit
    partial void OnShowDetailsChanged(bool value)
    {
        OnPropertyChanged(nameof(DetailsCollapsed));
        OnPropertyChanged(nameof(DetailsIcon));
    }
    [RelayCommand] private void ToggleDetails() => ShowDetails = !ShowDetails;

    // Collapse the "Add an item" form until the user wants it.
    [ObservableProperty] private bool showAddItemForm;
    public bool AddItemCollapsed => !ShowAddItemForm;
    public string AddItemIcon => ShowAddItemForm ? "✕" : "✎";   // pencil = edit
    partial void OnShowAddItemFormChanged(bool value)
    {
        OnPropertyChanged(nameof(AddItemCollapsed));
        OnPropertyChanged(nameof(AddItemIcon));
    }
    [RelayCommand] private void ToggleAddItemForm() => ShowAddItemForm = !ShowAddItemForm;

    // Collapsed summaries
    public string CategorySummary => CategoryChips.FirstOrDefault(c => c.Active)?.Name ?? "None";
    public string PeopleSummary
    {
        get
        {
            var names = _allPeople.Where(p => Draft.PeopleIds.Contains(p.Id)).Select(p => p.Name).ToList();
            return names.Count == 0 ? "No one yet" : string.Join(", ", names);
        }
    }
    public string PayerSummary =>
        _allPeople.FirstOrDefault(p => p.Id == Draft.PayerId)?.Name ?? "—";

    private void RaiseSummaries()
    {
        OnPropertyChanged(nameof(CategorySummary));
        OnPropertyChanged(nameof(PeopleSummary));
        OnPropertyChanged(nameof(PayerSummary));
    }

    // ---- total + items ----
    [ObservableProperty] private string totalText = string.Empty;
    [ObservableProperty] private string itemName = string.Empty;
    [ObservableProperty] private string itemPrice = string.Empty;

    // The person who paid — highlighted under the total.
    [ObservableProperty] private bool hasPayer;
    [ObservableProperty] private string payerName = string.Empty;
    [ObservableProperty] private string payerInitial = string.Empty;
    [ObservableProperty] private Color payerColor = Colors.Gray;

    public ObservableCollection<ToggleChip> CategoryChips { get; } = new();
    public ObservableCollection<ToggleChip> PeopleChips { get; } = new();
    public ObservableCollection<ToggleChip> PayerChips { get; } = new();
    public ObservableCollection<MenuRowVM> Items { get; } = new();

    // The editable TOTAL BILL AMOUNT is the authoritative bill total. Items
    // are portions carved out of it for people who didn't share them; the
    // remainder splits equally among everyone.
    public string EachLabel
    {
        get
        {
            var n = Draft.PeopleIds.Count;
            if (n == 0) return "Add people to split between";
            var total = Draft.Total;
            var cnt = Draft.Items.Count;
            var ppl = n == 1 ? "person" : "people";
            if (cnt == 0)
                return $"{BillMath.Money(total / n)} each · split between {n} {ppl}";
            var basePer = (total - Draft.ItemsSum) / n;
            return $"Base {BillMath.Money(basePer)} each · {n} {ppl} · {cnt} shared item{(cnt == 1 ? "" : "s")}";
        }
    }

    public CreateBillViewModel(DatabaseService db, DraftService drafts)
    {
        _db = db;
        _drafts = drafts;
    }

    public async Task LoadAsync()
    {
        PageTitle = Draft.BillId == 0 ? "New bill" : "Edit bill";
        FinishLabel = Draft.BillId == 0 ? "See who owes what" : "Save changes";
        BillName = Draft.Name;
        TotalText = Draft.TotalText;
        BillDate = Draft.Date.Date;
        BillTime = Draft.Date.TimeOfDay;
        ShowAddCategory = false;
        ShowAddPerson = false;
        ShowAddItemForm = false;
        ShowDetails = Draft.BillId == 0;   // new bill starts expanded; editing starts collapsed

        _allPeople = await _db.GetPeopleAsync();
        await BuildCategoryChipsAsync();
        BuildPeopleChips();
        RebuildPayerChips();
        UpdatePayerDisplay();
        RebuildItemRows();
        RaiseTotals();
        RaiseSummaries();
    }

    partial void OnBillNameChanged(string value) => Draft.Name = value;
    partial void OnBillDateChanged(DateTime value) => Draft.Date = value.Date + BillTime;
    partial void OnBillTimeChanged(TimeSpan value) => Draft.Date = BillDate.Date + value;

    partial void OnTotalTextChanged(string value)
    {
        Draft.TotalText = value;
        RaiseTotals();
    }

    private void RaiseTotals() => OnPropertyChanged(nameof(EachLabel));

    private List<Person> SelectedPeople()
        => _allPeople.Where(p => Draft.PeopleIds.Contains(p.Id)).ToList();

    // ---------- categories ----------
    private async Task BuildCategoryChipsAsync()
    {
        CategoryChips.Clear();
        CategoryChips.Add(MakeCatChip(0, "None"));
        foreach (var c in await _db.GetCategoriesAsync())
            CategoryChips.Add(MakeCatChip(c.Id, c.Name));
    }

    private ToggleChip MakeCatChip(int id, string name)
    {
        var chip = new ToggleChip
        {
            Id = id, Name = name, Variant = ChipVariant.Category,
            Active = (Draft.CategoryId ?? 0) == id,
        };
        chip.ToggleCommand = new RelayCommand(() => SelectCategory(chip));
        return chip;
    }

    private void SelectCategory(ToggleChip chip)
    {
        Draft.CategoryId = chip.Id == 0 ? null : chip.Id;
        foreach (var c in CategoryChips) c.Active = c.Id == chip.Id;
        RaiseSummaries();
    }

    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        var name = (NewCategoryName ?? "").Trim();
        if (name.Length == 0) return;
        var cat = await _db.SaveCategoryAsync(name);
        NewCategoryName = string.Empty;
        var chip = MakeCatChip(cat.Id, cat.Name);
        CategoryChips.Add(chip);
        SelectCategory(chip);
        ShowAddCategory = false;   // done — tuck the input away again
    }

    // ---------- people ----------
    private void BuildPeopleChips()
    {
        PeopleChips.Clear();
        foreach (var p in _allPeople)
        {
            var chip = new ToggleChip
            {
                Id = p.Id, Name = p.Name, HasAvatar = true, Variant = ChipVariant.Person,
                Initial = BillMath.Initial(p.Name),
                AvatarColor = Color.FromArgb(BillMath.ColorForIndex(p.ColorIndex)),
                Active = Draft.PeopleIds.Contains(p.Id),
                IsPayer = Draft.PayerId == p.Id,
            };
            chip.ToggleCommand = new RelayCommand(() => TogglePerson(chip));
            PeopleChips.Add(chip);
        }
    }

    private void TogglePerson(ToggleChip chip)
    {
        var has = Draft.PeopleIds.Contains(chip.Id);
        if (has)
        {
            Draft.PeopleIds.Remove(chip.Id);
            if (Draft.PayerId == chip.Id)
                Draft.PayerId = Draft.PeopleIds.Count > 0 ? Draft.PeopleIds[0] : null;
        }
        else
        {
            Draft.PeopleIds.Add(chip.Id);
            Draft.PayerId ??= chip.Id;
        }
        chip.Active = !has;
        AfterPeopleChanged();
    }

    [RelayCommand]
    private async Task AddPersonAsync()
    {
        var name = (NewPersonName ?? "").Trim();
        if (name.Length == 0) return;
        var person = await _db.SavePersonAsync(name);
        NewPersonName = string.Empty;
        _allPeople.Add(person);

        var chip = new ToggleChip
        {
            Id = person.Id, Name = person.Name, HasAvatar = true, Variant = ChipVariant.Person,
            Initial = BillMath.Initial(person.Name),
            AvatarColor = Color.FromArgb(BillMath.ColorForIndex(person.ColorIndex)),
            Active = true,
        };
        chip.ToggleCommand = new RelayCommand(() => TogglePerson(chip));
        PeopleChips.Add(chip);

        Draft.PeopleIds.Add(person.Id);
        Draft.PayerId ??= person.Id;
        AfterPeopleChanged();
    }

    private void AfterPeopleChanged()
    {
        RebuildPayerChips();
        RefreshCrowns();
        UpdatePayerDisplay();
        RebuildItemRows();
        HasPeople = Draft.PeopleIds.Count > 0;
        RaiseTotals();
        RaiseSummaries();
    }

    // ---------- payer ----------
    private void RebuildPayerChips()
    {
        PayerChips.Clear();
        foreach (var pid in Draft.PeopleIds)
        {
            var src = _allPeople.FirstOrDefault(p => p.Id == pid);
            var chip = new ToggleChip
            {
                Id = pid, Name = src?.Name ?? "?", HasAvatar = true, Variant = ChipVariant.Payer,
                Initial = BillMath.Initial(src?.Name),
                AvatarColor = Color.FromArgb(BillMath.ColorForIndex(src?.ColorIndex ?? 0)),
                Active = Draft.PayerId == pid,
                IsPayer = Draft.PayerId == pid,
            };
            chip.ToggleCommand = new RelayCommand(() => SelectPayer(chip));
            PayerChips.Add(chip);
        }
        HasPeople = Draft.PeopleIds.Count > 0;
    }

    private void SelectPayer(ToggleChip chip)
    {
        Draft.PayerId = chip.Id;
        foreach (var c in PayerChips) c.Active = c.Id == chip.Id;
        RefreshCrowns();
        UpdatePayerDisplay();
        RebuildItemRows();   // move the crown to the new payer
        RaiseSummaries();
    }

    // Keep the crown on the payer's chip in every chip row.
    private void RefreshCrowns()
    {
        foreach (var c in PeopleChips) c.IsPayer = c.Id == Draft.PayerId;
        foreach (var c in PayerChips) c.IsPayer = c.Id == Draft.PayerId;
    }

    private void UpdatePayerDisplay()
    {
        var payer = _allPeople.FirstOrDefault(p => p.Id == Draft.PayerId);
        HasPayer = payer is not null && Draft.PeopleIds.Contains(payer.Id);
        PayerName = payer?.Name ?? "";
        PayerInitial = BillMath.Initial(payer?.Name);
        PayerColor = Color.FromArgb(BillMath.ColorForIndex(payer?.ColorIndex ?? 0));
    }

    // ---------- items ----------
    private void RebuildItemRows()
    {
        Items.Clear();
        var people = SelectedPeople();
        foreach (var it in Draft.Items)
            Items.Add(new MenuRowVM(it, people, Draft.PayerId, RemoveRow));
    }

    [RelayCommand]
    private void AddItem()
    {
        var name = (ItemName ?? "").Trim();
        if (name.Length == 0) return;
        if (!decimal.TryParse(ItemPrice, out var price) || price <= 0) return;

        var item = new DraftItem { Name = name, Price = price };
        Draft.Items.Add(item);
        Items.Add(new MenuRowVM(item, SelectedPeople(), Draft.PayerId, RemoveRow));

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

    // ---------- finish ----------
    [RelayCommand]
    private async Task FinishAsync()
    {
        var d = Draft;
        if (d.PeopleIds.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync("Add people",
                "Pick at least one person to split with.", "OK");
            return;
        }
        if (d.Total <= 0)
        {
            await Shell.Current.DisplayAlertAsync("Enter a total",
                "Enter the total bill amount before splitting.", "OK");
            return;
        }

        d.PayerId ??= d.PeopleIds.FirstOrDefault();

        // Drop exclusions that reference people no longer on the bill.
        var items = d.Items
            .Select(i => (i.Name, i.Price,
                i.Excluded.Where(id => d.PeopleIds.Contains(id)).ToList()))
            .ToList();

        var isNew = d.BillId == 0;
        var bill = new Bill
        {
            Id = d.BillId,
            Name = string.IsNullOrWhiteSpace(d.Name) ? "Untitled bill" : d.Name.Trim(),
            CategoryId = d.CategoryId ?? 0,
            PayerId = d.PayerId ?? 0,
            Amount = d.Total,
            Date = d.Date,
            IsDone = d.IsDone,
            CreatedDate = d.CreatedDate,
        };

        var id = await _db.SaveBillAsync(bill, d.PeopleIds, items);
        d.BillId = id;   // keep the draft so the user can step back and edit
        await Shell.Current.GoToAsync($"ResultPage?billId={id}&created={(isNew ? 1 : 0)}");
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}
