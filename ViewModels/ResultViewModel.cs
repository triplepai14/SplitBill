using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SplitBillApp.Data;
using SplitBillApp.Services;

namespace SplitBillApp.ViewModels;

public class ResultRowVM
{
    public string Name { get; set; } = "";
    public string Initial { get; set; } = "";
    public Color AvatarColor { get; set; } = Colors.Gray;
    public string Sub { get; set; } = "";
    public string AmountLabel { get; set; } = "";
    public Color AmountColor { get; set; } = Color.FromArgb("#16201A");
    public string Tag { get; set; } = "";
    public Color RowBg { get; set; } = Color.FromArgb("#FFFFFF");
    public Color RowStroke { get; set; } = Color.FromArgb("#E7EAE4");
}

// One read-only line of the bill breakdown: an item (or the equally-shared
// remainder), its price, and who shared it.
public class BillLineVM
{
    public string Name { get; set; } = "";
    public string PriceLabel { get; set; } = "";
    public string Sub { get; set; } = "";
}

public class SettlementVM
{
    public string FromInitial { get; set; } = "";
    public Color FromColor { get; set; } = Colors.Gray;
    public string FromName { get; set; } = "";
    public string ToInitial { get; set; } = "";
    public Color ToColor { get; set; } = Colors.Gray;
    public string ToName { get; set; } = "";
    public string AmountLabel { get; set; } = "";
}

[QueryProperty(nameof(BillId), "billId")]
[QueryProperty(nameof(Created), "created")]
public partial class ResultViewModel : BaseViewModel
{
    private readonly DatabaseService _db;
    private readonly DraftService _drafts;

    static readonly Color Accent = Color.FromArgb("#1F8A5B");
    static readonly Color AccentSoft = Color.FromArgb("#E4F1EA");
    static readonly Color Surface = Color.FromArgb("#FFFFFF");
    static readonly Color Line = Color.FromArgb("#E7EAE4");
    static readonly Color TextC = Color.FromArgb("#16201A");

    [ObservableProperty] private int billId;
    [ObservableProperty] private string created = string.Empty;

    [ObservableProperty] private string resultTitle = "";
    [ObservableProperty] private string totalLabel = "";
    [ObservableProperty] private string paidByLabel = "";
    [ObservableProperty] private string perHeadLabel = "";
    [ObservableProperty] private string modifiedLabel = "";
    [ObservableProperty] private bool hasSettlements;
    [ObservableProperty] private bool hasLines;
    [ObservableProperty] private string doneLabel = "Back to bills";

    public ObservableCollection<ResultRowVM> Rows { get; } = new();
    public ObservableCollection<BillLineVM> Lines { get; } = new();
    public ObservableCollection<SettlementVM> Settlements { get; } = new();

    public ResultViewModel(DatabaseService db, DraftService drafts)
    {
        _db = db;
        _drafts = drafts;
    }

    partial void OnBillIdChanged(int value) => _ = LoadAsync();

    public async Task LoadAsync()
    {
        if (BillId == 0) return;
        var detail = await _db.GetBillDetailAsync(BillId);
        if (detail is null) return;

        var bill = detail.Bill;
        var total = detail.Total;
        var shares = detail.Shares;
        var payerId = bill.PayerId;

        ResultTitle = bill.Name;
        TotalLabel = BillMath.Money(total);
        PaidByLabel = $"Paid by 👑 {bill.PayerName}";
        PerHeadLabel = detail.People.Count > 0
            ? $"avg {BillMath.Money(total / detail.People.Count)}" : "";
        DoneLabel = Created == "1" ? "Done · save bill" : "Back to bills";

        // Rows saved before the ModifiedDate column existed read as default —
        // fall back to the creation date for those.
        var modified = bill.ModifiedDate == default ? bill.CreatedDate : bill.ModifiedDate;
        var edited = (modified - bill.CreatedDate).TotalSeconds > 1;
        ModifiedLabel = $"{(edited ? "Updated" : "Created")} {modified:d MMM yyyy · HH:mm}";

        // Read-only breakdown of what's on the bill: each carve-out item with
        // who shared it, then whatever's left as the equally-shared remainder.
        Lines.Clear();
        decimal carvedOut = 0;
        foreach (var it in detail.Items)
        {
            detail.ExclusionsByItem.TryGetValue(it.Id, out var ex);
            ex ??= new HashSet<int>();
            var included = detail.People.Where(p => !ex.Contains(p.Id)).ToList();
            if (included.Count == 0) continue;   // nobody shared it → stays in the base

            carvedOut += it.Price;
            var excludedNames = detail.People.Where(p => ex.Contains(p.Id)).Select(p => p.Name).ToList();
            Lines.Add(new BillLineVM
            {
                Name = it.Name,
                PriceLabel = BillMath.Money(it.Price),
                Sub = excludedNames.Count == 0
                    ? $"Everyone · split {included.Count} ways"
                    : $"Without {string.Join(", ", excludedNames)} · split {included.Count} way{(included.Count == 1 ? "" : "s")}",
            });
        }
        if (Lines.Count > 0)
        {
            var baseAmount = total - carvedOut;
            if (baseAmount > 0.005m)
                Lines.Add(new BillLineVM
                {
                    Name = "Everything else",
                    PriceLabel = BillMath.Money(baseAmount),
                    Sub = $"Everyone · split {detail.People.Count} ways",
                });
        }
        HasLines = Lines.Count > 0;

        // Payer first, then everyone else in order.
        var ordered = detail.People
            .OrderByDescending(p => p.Id == payerId)
            .ToList();

        Rows.Clear();
        foreach (var p in ordered)
        {
            var owe = shares.GetValueOrDefault(p.Id, 0m);
            var isPayer = p.Id == payerId;
            Rows.Add(new ResultRowVM
            {
                Name = isPayer ? $"👑 {p.Name}" : p.Name,
                Initial = BillMath.Initial(p.Name),
                AvatarColor = Color.FromArgb(BillMath.ColorForIndex(p.ColorIndex)),
                Sub = isPayer
                    ? $"Paid {BillMath.Money(total)} · share {BillMath.Money(owe)}"
                    : "Their share of the bill",
                AmountLabel = isPayer ? $"+{BillMath.Money(total - owe)}" : BillMath.Money(owe),
                AmountColor = isPayer ? Accent : TextC,
                Tag = isPayer ? "gets back" : "owes",
                RowBg = isPayer ? AccentSoft : Surface,
                RowStroke = isPayer ? Accent : Line,
            });
        }

        Settlements.Clear();
        var payer = detail.People.FirstOrDefault(p => p.Id == payerId);
        foreach (var p in detail.People.Where(p => p.Id != payerId))
        {
            var owe = shares.GetValueOrDefault(p.Id, 0m);
            if (owe <= 0.001m) continue;
            Settlements.Add(new SettlementVM
            {
                FromInitial = BillMath.Initial(p.Name),
                FromColor = Color.FromArgb(BillMath.ColorForIndex(p.ColorIndex)),
                FromName = p.Name,
                ToInitial = BillMath.Initial(payer?.Name ?? "?"),
                ToColor = Color.FromArgb(BillMath.ColorForIndex(payer?.ColorIndex ?? 0)),
                ToName = $"👑 {payer?.Name ?? "?"}",
                AmountLabel = BillMath.Money(owe),
            });
        }
        HasSettlements = Settlements.Count > 0;
    }

    // Big primary button: finish and return home, discarding any draft.
    [RelayCommand]
    private async Task DoneAsync()
    {
        _drafts.Clear();
        await Shell.Current.GoToAsync("//HomePage");
    }

    // Header chevron: step back one page (to the edit step, or home).
    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");

    // Load this bill into an editable draft and open the create flow.
    [RelayCommand]
    private async Task EditAsync()
    {
        var detail = await _db.GetBillDetailAsync(BillId);
        if (detail is null) return;
        _drafts.StartEdit(detail);
        await Shell.Current.GoToAsync("CreateBillPage");
    }

    // Share the bill as a short text summary (total, who paid, who pays whom
    // back) — pick LINE or any chat app from the share sheet to send it.
    [RelayCommand]
    private async Task ShareAsync()
    {
        var detail = await _db.GetBillDetailAsync(BillId);
        if (detail is null) return;
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = ResultTitle,
            Text = SummaryText.ForBill(detail),
        });
    }

    // Permanently remove this bill (after confirmation) and return home.
    [RelayCommand]
    private async Task DeleteAsync()
    {
        var confirmed = await Shell.Current.DisplayAlertAsync("Delete bill?",
            $"\"{ResultTitle}\" will be removed permanently.", "Delete", "Cancel");
        if (!confirmed) return;

        await _db.DeleteBillAsync(BillId);
        _drafts.Clear();
        await Shell.Current.GoToAsync("//HomePage");
    }
}
