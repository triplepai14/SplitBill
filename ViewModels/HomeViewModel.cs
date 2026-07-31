using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SplitBillApp.Data;
using SplitBillApp.Services;

namespace SplitBillApp.ViewModels;

// A bill row — shown nested under a category group (real or "Uncategorized").
public partial class BillRowVM : ObservableObject
{
    public int BillId { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = "";
    public string PaidByLabel { get; set; } = "";
    public string TotalLabel { get; set; } = "";
    public string CountLabel { get; set; } = "";
    public List<AvatarVM> Avatars { get; set; } = new();

    // Pending bills are tinted green; settled ones go light grey.
    public Color CardBg { get; set; } = Colors.White;
    public Color CardSub { get; set; } = Color.FromArgb("#6A756F");

    public bool CanToggleDone { get; set; }          // only uncategorized bills
    [ObservableProperty] private bool isDone;
    public string DoneLabel => IsDone ? "↩" : "✓";
    partial void OnIsDoneChanged(bool value) => OnPropertyChanged(nameof(DoneLabel));

    public ICommand? OpenCommand { get; set; }       // tap the card -> edit
    public ICommand? ViewCommand { get; set; }       // "View ›" -> the split detail
    public ICommand? DeleteCommand { get; set; }
    public ICommand? DoneCommand { get; set; }
}

// A collapsible group: a real category, or the special "Uncategorized" bucket.
public partial class CategoryGroupVM : ObservableObject
{
    static readonly Color Surface2 = Color.FromArgb("#EDE0CE");        // light brown — Uncategorized
    static readonly Color SpecialStroke = Color.FromArgb("#D8C3A5");
    static readonly Color Accent = Color.FromArgb("#1F8A5B");       // brand green — still open (Pending tab)
    static readonly Color AccentDark = Color.FromArgb("#166F4A");   // darker green (drag-over / edge)
    static readonly Color DoneBgC = Color.FromArgb("#2E3D34");      // deep ink — settled (Done tab)
    static readonly Color DoneDark = Color.FromArgb("#212C25");     // darker ink (drag-over / edge)
    static readonly Color Line = Color.FromArgb("#E7EAE4");
    static readonly Color TextC = Color.FromArgb("#16201A");
    static readonly Color Sub = Color.FromArgb("#6A756F");
    static readonly Color SubOnGreen = Color.FromArgb("#CFE7DA");   // muted text on green
    static readonly Color SubOnInk = Color.FromArgb("#C3D0C7");     // muted text on ink
    static readonly Color SpecialDragBg = Color.FromArgb("#DFC9A8");   // deeper brown while dragging over

    public int CategoryId { get; set; }
    public bool IsSpecial { get; set; }              // the Uncategorized bucket
    public string Name { get; set; } = "";
    public string CountLabel { get; set; } = "";
    public string TotalLabel { get; set; } = "";
    public bool IsDone { get; set; }
    public bool ShowActions => !IsSpecial;           // total/view only for real categories
    // Only settled categories offer a toggle (to reopen); marking one done
    // happens on its summary page.
    public bool ShowDoneToggle => !IsSpecial && IsDone;
    public string DoneLabel => IsDone ? "↩ Reopen" : "✓ Done";

    public ObservableCollection<BillRowVM> Bills { get; } = new();

    [ObservableProperty] private bool isExpanded = true;
    public string Chevron => IsExpanded ? "▾" : "▸";
    partial void OnIsExpandedChanged(bool value) => OnPropertyChanged(nameof(Chevron));

    [ObservableProperty] private bool isDragOver;
    // Categories get a dark header with light text so they stand out from the
    // bill cards: brand green while pending, deep ink once settled.
    // The Uncategorized bucket stays neutral grey.
    public Color HeaderBg => IsSpecial
        ? (IsDragOver ? SpecialDragBg : Surface2)
        : IsDone
            ? (IsDragOver ? DoneDark : DoneBgC)
            : (IsDragOver ? AccentDark : Accent);
    public Color HeaderStroke => IsSpecial
        ? SpecialStroke
        : (IsDone ? DoneDark : AccentDark);
    public Color TitleColor => IsSpecial ? TextC : Colors.White;
    public Color SubColor => IsSpecial ? Sub : (IsDone ? SubOnInk : SubOnGreen);
    partial void OnIsDragOverChanged(bool value)
    {
        OnPropertyChanged(nameof(HeaderBg));
        OnPropertyChanged(nameof(HeaderStroke));
    }

    public ICommand? ToggleCommand { get; set; }
    public ICommand? OpenSummaryCommand { get; set; }
    public ICommand? DoneCommand { get; set; }
}

public partial class HomeViewModel : BaseViewModel
{
    private readonly DatabaseService _db;
    private readonly DraftService _drafts;

    public ObservableCollection<CategoryGroupVM> Groups { get; } = new();

    [ObservableProperty] private bool isEmpty;

    // Tabs
    [ObservableProperty] private bool showDone;
    public bool ShowPending => !ShowDone;
    public Color PendingBg => ShowPending ? Accent : Colors.Transparent;
    public Color PendingText => ShowPending ? Colors.White : Sub;
    public Color DoneBg => ShowDone ? Accent : Colors.Transparent;
    public Color DoneText => ShowDone ? Colors.White : Sub;
    private static Color Accent => Color.FromArgb("#1F8A5B");
    private static Color Sub => Color.FromArgb("#6A756F");

    // Speed-dial FAB
    [ObservableProperty] private bool isMenuOpen;
    public string FabIcon => IsMenuOpen ? "×" : "+";
    partial void OnIsMenuOpenChanged(bool value) => OnPropertyChanged(nameof(FabIcon));

    public HomeViewModel(DatabaseService db, DraftService drafts)
    {
        _db = db;
        _drafts = drafts;
    }

    partial void OnShowDoneChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowPending));
        OnPropertyChanged(nameof(PendingBg));
        OnPropertyChanged(nameof(PendingText));
        OnPropertyChanged(nameof(DoneBg));
        OnPropertyChanged(nameof(DoneText));
        _ = LoadAsync();
    }

    [RelayCommand] private void ShowPendingTab() => ShowDone = false;
    [RelayCommand] private void ShowDoneTab() => ShowDone = true;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;

            var bills = await _db.GetBillsAsync();
            var cats = await _db.GetCategoriesAsync();

            Groups.Clear();

            // Real categories that match the active tab.
            foreach (var c in cats.Where(c => c.IsDone == ShowDone))
            {
                var group = new CategoryGroupVM
                {
                    CategoryId = c.Id,
                    Name = c.Name,
                    IsDone = c.IsDone,
                    CountLabel = $"{c.Date:d MMM} · {c.BillCount} bill{(c.BillCount == 1 ? "" : "s")} · {c.People.Count} people",
                    TotalLabel = BillMath.Money(c.Total),
                };
                var cid = c.Id;
                group.ToggleCommand = new RelayCommand(() => group.IsExpanded = !group.IsExpanded);
                group.OpenSummaryCommand = new AsyncRelayCommand(() =>
                    Shell.Current.GoToAsync($"CategoryPage?categoryId={cid}"));
                group.DoneCommand = new AsyncRelayCommand(() => ToggleCategoryDoneAsync(cid, !c.IsDone));

                foreach (var b in bills.Where(b => b.CategoryId == c.Id))
                    group.Bills.Add(MakeBillRow(b));
                Groups.Add(group);
            }

            // Uncategorized bucket (bills with no category) for the active tab.
            var uncat = bills.Where(b => b.CategoryId == 0 && b.IsDone == ShowDone).ToList();
            if (uncat.Count > 0 || ShowPending)
            {
                var group = new CategoryGroupVM
                {
                    CategoryId = 0,
                    IsSpecial = true,
                    Name = "Uncategorized",
                    CountLabel = uncat.Count > 0
                        ? $"{uncat.Count} bill{(uncat.Count == 1 ? "" : "s")}"
                        : "Drop a bill here to remove it from a category",
                };
                group.ToggleCommand = new RelayCommand(() => group.IsExpanded = !group.IsExpanded);
                foreach (var b in uncat) group.Bills.Add(MakeBillRow(b));
                Groups.Add(group);
            }

            IsEmpty = bills.Count == 0 && cats.Count == 0;
        }
        finally { IsBusy = false; }
    }

    private BillRowVM MakeBillRow(Models.Bill b)
    {
        var avatars = b.People.Take(3).Select((p, i) =>
            AvatarVM.For(p.Name, p.ColorIndex, size: 28,
                marginLeft: i == 0 ? 0 : -8, borderWidth: 2)).ToList();

        var row = new BillRowVM
        {
            BillId = b.Id,
            CategoryId = b.CategoryId,
            Name = b.Name,
            PaidByLabel = $"Paid by 👑 {b.PayerName}",
            TotalLabel = BillMath.Money(b.Total),
            CountLabel = $"{b.People.Count} people",
            Avatars = avatars,
            CanToggleDone = b.CategoryId == 0,
            IsDone = b.IsDone,
            CardBg = ShowDone ? Color.FromArgb("#E4E8E2") : Color.FromArgb("#99CC9D"),
            CardSub = ShowDone ? Color.FromArgb("#6A756F") : Color.FromArgb("#33452F"),
        };
        row.OpenCommand = new AsyncRelayCommand(() => EditBillAsync(row.BillId));
        row.ViewCommand = new AsyncRelayCommand(() =>
            Shell.Current.GoToAsync($"ResultPage?billId={row.BillId}"));
        row.DeleteCommand = new AsyncRelayCommand(() => DeleteBillAsync(row.BillId, row.Name));
        row.DoneCommand = new AsyncRelayCommand(async () =>
        {
            await _db.SetBillDoneAsync(row.BillId, !row.IsDone);
            await LoadAsync();
        });
        return row;
    }

    // Tapping a bill opens it in the edit form.
    private async Task EditBillAsync(int billId)
    {
        var detail = await _db.GetBillDetailAsync(billId);
        if (detail is null) return;
        _drafts.StartEdit(detail);
        await Shell.Current.GoToAsync("CreateBillPage");
    }

    // Called from the drag & drop drop handler in the code-behind.
    public async Task MoveBillAsync(BillRowVM bill, int categoryId)
    {
        if (bill.CategoryId == categoryId) return;
        await _db.SetBillCategoryAsync(bill.BillId, categoryId);
        await LoadAsync();
    }

    private async Task ToggleCategoryDoneAsync(int categoryId, bool done)
    {
        await _db.SetCategoryDoneAsync(categoryId, done);
        await LoadAsync();
    }

    // ---------- speed-dial FAB ----------
    [RelayCommand] private void ToggleMenu() => IsMenuOpen = !IsMenuOpen;

    [RelayCommand]
    private async Task NewBillAsync()
    {
        IsMenuOpen = false;
        _drafts.Start();
        await Shell.Current.GoToAsync("CreateBillPage");
    }

    [RelayCommand]
    private async Task NewCategoryAsync()
    {
        IsMenuOpen = false;
        var name = await Shell.Current.DisplayPromptAsync(
            "New category", "Name", "Add", "Cancel", "e.g. Bali Trip");
        if (string.IsNullOrWhiteSpace(name)) return;
        await _db.SaveCategoryAsync(name.Trim());
        await LoadAsync();
    }

    // ---------- per-bill actions ----------
    private async Task DeleteBillAsync(int billId, string name)
    {
        var confirmed = await Shell.Current.DisplayAlertAsync("Delete bill?",
            $"\"{name}\" will be removed permanently.", "Delete", "Cancel");
        if (!confirmed) return;
        await _db.DeleteBillAsync(billId);
        await LoadAsync();
    }

}
