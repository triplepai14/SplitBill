using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SplitBillApp.Data;
using SplitBillApp.Services;

namespace SplitBillApp.ViewModels;

public partial class CreateBillViewModel : BaseViewModel
{
    private readonly DatabaseService _db;
    private readonly DraftService _drafts;

    private BillDraft Draft => _drafts.Current ?? _drafts.Start();

    [ObservableProperty] private string billName = string.Empty;
    [ObservableProperty] private string newCategoryName = string.Empty;
    [ObservableProperty] private string newPersonName = string.Empty;
    [ObservableProperty] private bool hasPeople;

    public ObservableCollection<ToggleChip> CategoryChips { get; } = new();
    public ObservableCollection<ToggleChip> PeopleChips { get; } = new();
    public ObservableCollection<ToggleChip> PayerChips { get; } = new();

    public bool CanContinue => Draft.PeopleIds.Count > 0;
    public double ContinueOpacity => CanContinue ? 1 : 0.45;

    public CreateBillViewModel(DatabaseService db, DraftService drafts)
    {
        _db = db;
        _drafts = drafts;
    }

    public async Task LoadAsync()
    {
        BillName = Draft.Name;
        await BuildCategoryChipsAsync();
        await BuildPeopleChipsAsync();
        RebuildPayerChips();
    }

    partial void OnBillNameChanged(string value) => Draft.Name = value;

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
    }

    // ---------- people ----------
    private async Task BuildPeopleChipsAsync()
    {
        PeopleChips.Clear();
        foreach (var p in await _db.GetPeopleAsync())
        {
            var chip = new ToggleChip
            {
                Id = p.Id, Name = p.Name, HasAvatar = true, Variant = ChipVariant.Person,
                Initial = BillMath.Initial(p.Name),
                AvatarColor = Color.FromArgb(BillMath.ColorForIndex(p.ColorIndex)),
                Active = Draft.PeopleIds.Contains(p.Id),
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
                Draft.PayerId = Draft.PeopleIds.FirstOrDefault();
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
        HasPeople = Draft.PeopleIds.Count > 0;
        OnPropertyChanged(nameof(CanContinue));
        OnPropertyChanged(nameof(ContinueOpacity));
    }

    // ---------- payer ----------
    private void RebuildPayerChips()
    {
        PayerChips.Clear();
        foreach (var pid in Draft.PeopleIds)
        {
            var src = PeopleChips.FirstOrDefault(c => c.Id == pid);
            var chip = new ToggleChip
            {
                Id = pid, Name = src?.Name ?? "?", HasAvatar = true, Variant = ChipVariant.Payer,
                Initial = src?.Initial ?? "?",
                AvatarColor = src?.AvatarColor ?? Colors.Gray,
                Active = Draft.PayerId == pid,
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
    }

    // ---------- nav ----------
    [RelayCommand]
    private async Task ContinueAsync()
    {
        if (!CanContinue) return;
        Draft.PayerId ??= Draft.PeopleIds.FirstOrDefault();
        await Shell.Current.GoToAsync("MenusPage");
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}
