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
    [ObservableProperty] private bool hasSettlements;
    [ObservableProperty] private string doneLabel = "Back to bills";

    public ObservableCollection<ResultRowVM> Rows { get; } = new();
    public ObservableCollection<SettlementVM> Settlements { get; } = new();

    public ResultViewModel(DatabaseService db) => _db = db;

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
        PaidByLabel = $"Paid by {bill.PayerName}";
        PerHeadLabel = detail.People.Count > 0
            ? $"avg {BillMath.Money(total / detail.People.Count)}" : "";
        DoneLabel = Created == "1" ? "Done · save bill" : "Back to bills";

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
                Name = p.Name,
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
                ToName = payer?.Name ?? "?",
                AmountLabel = BillMath.Money(owe),
            });
        }
        HasSettlements = Settlements.Count > 0;
    }

    [RelayCommand]
    private async Task DoneAsync() => await Shell.Current.GoToAsync("//HomePage");
}
