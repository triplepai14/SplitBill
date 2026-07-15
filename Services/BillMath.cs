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
    /// How much each participant owes.
    ///
    /// The bill's <paramref name="total"/> is authoritative. Any "items" are
    /// portions carved OUT of that total that some people didn't share — each
    /// item's price is split only among the people included in it. Whatever is
    /// left of the total after removing the items (the "base") is split equally
    /// among everyone.
    ///
    /// e.g. total 1000, people A/B/C, item "Coke" 100 excluding A →
    ///   Coke 100 / 2 (B,C) = 50 each; base 900 / 3 = 300 each →
    ///   A 300, B 350, C 350.  (Shares always sum back to the total.)
    /// </summary>
    public static Dictionary<int, decimal> Shares(
        IReadOnlyList<int> peopleIds,
        decimal total,
        IReadOnlyList<BillItem> items,
        IReadOnlyDictionary<int, HashSet<int>> exclusionsByItem)
    {
        var map = peopleIds.ToDictionary(id => id, _ => 0m);
        if (peopleIds.Count == 0) return map;

        // Carve out each item among the people who shared it.
        decimal itemsSum = 0;
        foreach (var item in items)
        {
            exclusionsByItem.TryGetValue(item.Id, out var excluded);
            excluded ??= new HashSet<int>();

            var included = peopleIds.Where(p => !excluded.Contains(p)).ToList();
            if (included.Count == 0) continue;   // nobody shared it → leave it in the base

            itemsSum += item.Price;
            var per = Math.Round(item.Price / included.Count, 2);
            foreach (var p in included) map[p] += per;
            map[included[0]] += item.Price - per * included.Count;
        }

        // Split whatever remains of the total equally among everyone.
        var baseAmount = total - itemsSum;
        var basePer = Math.Round(baseAmount / peopleIds.Count, 2);
        foreach (var p in peopleIds) map[p] += basePer;
        map[peopleIds[0]] += baseAmount - basePer * peopleIds.Count;

        return map;
    }

    /// <summary>
    /// Turns net balances (positive = should get money back, negative = owes)
    /// into a short list of "X pays Y" transfers, using greedy max-debtor /
    /// max-creditor matching. Used for the whole-category settle-up.
    /// </summary>
    public static List<(int FromId, int ToId, decimal Amount)> Settle(
        IReadOnlyDictionary<int, decimal> netBalances)
    {
        var debtors = netBalances.Where(kv => kv.Value < -0.009m)
            .Select(kv => (Id: kv.Key, Amount: -kv.Value))
            .OrderByDescending(x => x.Amount).ToList();
        var creditors = netBalances.Where(kv => kv.Value > 0.009m)
            .Select(kv => (Id: kv.Key, Amount: kv.Value))
            .OrderByDescending(x => x.Amount).ToList();

        var transfers = new List<(int, int, decimal)>();
        int di = 0, ci = 0;
        while (di < debtors.Count && ci < creditors.Count)
        {
            var pay = Math.Round(Math.Min(debtors[di].Amount, creditors[ci].Amount), 2);
            if (pay > 0)
                transfers.Add((debtors[di].Id, creditors[ci].Id, pay));

            debtors[di] = (debtors[di].Id, debtors[di].Amount - pay);
            creditors[ci] = (creditors[ci].Id, creditors[ci].Amount - pay);
            if (debtors[di].Amount < 0.009m) di++;
            if (creditors[ci].Amount < 0.009m) ci++;
        }
        return transfers;
    }
}
