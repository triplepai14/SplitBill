namespace SplitBillApp.Services;

// One line being added to a draft bill, with the set of people excluded from it.
public class DraftItem
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public HashSet<int> Excluded { get; set; } = new();
}

// The bill currently being created, shared between the Create and Menus pages.
public class BillDraft
{
    public string Name { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public List<int> PeopleIds { get; set; } = new();
    public int? PayerId { get; set; }

    public bool Flat { get; set; } = true;         // "Split a total" vs "Itemize"
    public string FlatTotal { get; set; } = string.Empty;
    public List<DraftItem> Items { get; set; } = new();

    public decimal RunningTotal =>
        Flat ? (decimal.TryParse(FlatTotal, out var t) ? t : 0m)
             : Items.Sum(i => i.Price);
}

/// <summary>
/// Holds the draft bill in memory while the user walks through the create flow.
/// Registered as a singleton so the Create and Menus pages edit the same object.
/// </summary>
public class DraftService
{
    public BillDraft? Current { get; private set; }

    public BillDraft Start()
    {
        Current = new BillDraft();
        return Current;
    }

    public void Clear() => Current = null;
}
