namespace SplitBillApp.Services;

public record Settlement(string FromName, string ToName, decimal Amount);

/// <summary>
/// Turns a set of net balances into a minimal list of "X pays Y" transactions.
/// Uses a greedy max-debtor / max-creditor matching, which produces a near-minimal
/// number of payments and is plenty for a real-world bill-splitting app.
/// </summary>
public static class SettlementCalculator
{
    public static List<Settlement> Calculate(
        Dictionary<int, string> names,
        Dictionary<int, decimal> netBalances)
    {
        var settlements = new List<Settlement>();

        // Work on a mutable copy, ignoring anyone who is settled (~0).
        var debtors = netBalances
            .Where(kv => kv.Value < -0.009m)
            .Select(kv => (Id: kv.Key, Amount: kv.Value)) // negative
            .OrderBy(x => x.Amount)
            .ToList();

        var creditors = netBalances
            .Where(kv => kv.Value > 0.009m)
            .Select(kv => (Id: kv.Key, Amount: kv.Value)) // positive
            .OrderByDescending(x => x.Amount)
            .ToList();

        int di = 0, ci = 0;
        while (di < debtors.Count && ci < creditors.Count)
        {
            var debtor = debtors[di];
            var creditor = creditors[ci];

            var owed = -debtor.Amount;                 // how much debtor still owes
            var pay = Math.Min(owed, creditor.Amount); // settle the smaller side
            pay = Math.Round(pay, 2);

            if (pay > 0)
            {
                settlements.Add(new Settlement(
                    names.GetValueOrDefault(debtor.Id, "Unknown"),
                    names.GetValueOrDefault(creditor.Id, "Unknown"),
                    pay));
            }

            debtor = (debtor.Id, debtor.Amount + pay);
            creditor = (creditor.Id, creditor.Amount - pay);
            debtors[di] = debtor;
            creditors[ci] = creditor;

            if (Math.Abs(debtor.Amount) < 0.009m) di++;
            if (Math.Abs(creditor.Amount) < 0.009m) ci++;
        }

        return settlements;
    }
}
