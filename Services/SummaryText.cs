using SplitBillApp.Data;

namespace SplitBillApp.Services;

/// <summary>
/// Builds the short share texts (kept deliberately minimal: total, who paid,
/// who pays whom back). Used by the share buttons on every page.
/// </summary>
public static class SummaryText
{
    public static string ForBill(BillDetail d)
    {
        var lines = new List<string>
        {
            $"{d.Bill.Name} — {BillMath.Money(d.Total)}",
            $"Paid by 👑 {d.Bill.PayerName}",
        };

        var transfers = d.People
            .Where(p => p.Id != d.Bill.PayerId)
            .Select(p => (p.Name, Amount: d.Shares.GetValueOrDefault(p.Id, 0m)))
            .Where(t => t.Amount > 0.001m)
            .ToList();
        if (transfers.Count > 0)
        {
            lines.Add("");
            lines.AddRange(transfers.Select(t =>
                $"{t.Name} → 👑 {d.Bill.PayerName}: {BillMath.Money(t.Amount)}"));
        }
        return string.Join(Environment.NewLine, lines);
    }

    public static string ForCategory(CategoryStats s)
    {
        var lines = new List<string>
        {
            $"{s.Category.Name} — {BillMath.Money(s.Total)} · " +
            $"{s.Bills.Count} bill{(s.Bills.Count == 1 ? "" : "s")} · " +
            $"{s.People.Count} people",
        };
        lines.AddRange(s.Bills.Select(b =>
            $"- {b.Name} {BillMath.Money(b.Total)} · Paid by 👑 {b.PayerName}"));

        if (s.Settlements.Count > 0)
        {
            lines.Add("");
            lines.AddRange(s.Settlements.Select(t =>
                $"{t.From.Name} → {t.To.Name}: {BillMath.Money(t.Amount)}"));
        }
        return string.Join(Environment.NewLine, lines);
    }
}
