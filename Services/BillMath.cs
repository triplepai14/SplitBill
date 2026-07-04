using SplitBillApp.Models;

namespace SplitBillApp.Services;

/// <summary>
/// Pure helpers shared across the app: the avatar palette, money formatting,
/// and the per-person split maths. No I/O — everything here is deterministic.
/// </summary>
public static class BillMath
{
    // Avatar palette, matching the design's colour order.
    public static readonly string[] Palette =
    {
        "#1F8A5B", "#2A6FDB", "#CF6A45", "#7C4DDB", "#C79100", "#0E9B8E", "#D2517A"
    };

    public static string ColorForIndex(int i)
        => Palette[((i % Palette.Length) + Palette.Length) % Palette.Length];

    public static string Initial(string? name)
    {
        var n = (name ?? "").Trim();
        return n.Length == 0 ? "?" : char.ToUpperInvariant(n[0]).ToString();
    }

    // ฿1,200  or  ฿12.50 — whole numbers drop the decimals.
    public static string Money(decimal n)
    {
        var v = Math.Round(n, 2);
        return "฿" + (v == Math.Truncate(v) ? v.ToString("#,##0") : v.ToString("#,##0.00"));
    }

    /// <summary>
    /// How much each participant owes. Every item's price is divided equally
    /// among the people included in that item (participants minus that item's
    /// exclusions). Rounding remainders are pushed onto the first included
    /// person so the shares always sum back to the item price.
    /// </summary>
    public static Dictionary<int, decimal> Shares(
        IReadOnlyList<int> peopleIds,
        IReadOnlyList<BillItem> items,
        IReadOnlyDictionary<int, HashSet<int>> exclusionsByItem)
    {
        var map = peopleIds.ToDictionary(id => id, _ => 0m);
        foreach (var item in items)
        {
            exclusionsByItem.TryGetValue(item.Id, out var excluded);
            excluded ??= new HashSet<int>();

            var included = peopleIds.Where(p => !excluded.Contains(p)).ToList();
            if (included.Count == 0) continue;

            var per = Math.Round(item.Price / included.Count, 2);
            foreach (var p in included) map[p] += per;

            var remainder = item.Price - per * included.Count;
            map[included[0]] += remainder;
        }
        return map;
    }
}
