using SplitBillApp.Data;

namespace SplitBillApp.Services;

// One line being added to a draft bill, with the set of people excluded from it.
public class DraftItem
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public HashSet<int> Excluded { get; set; } = new();
}

// The bill currently being created or edited, shared between the create/edit
// page and the Result page.
public class BillDraft
{
    // 0 = a brand-new bill; otherwise the id of the bill being edited.
    public int BillId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime Date { get; set; } = DateTime.Now;   // the bill's editable date
    public bool IsDone { get; set; }

    public string Name { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public List<int> PeopleIds { get; set; } = new();
    public int? PayerId { get; set; }

    // The editable TOTAL BILL AMOUNT plus optional carve-out items.
    public string TotalText { get; set; } = string.Empty;
    public List<DraftItem> Items { get; set; } = new();

    public decimal Total => decimal.TryParse(TotalText, out var t) ? t : 0m;
    public decimal ItemsSum => Items.Sum(i => i.Price);
}

/// <summary>
/// Holds the draft bill in memory while the user walks through the create flow.
/// Registered as a singleton so every page in the flow edits the same object.
/// </summary>
public class DraftService
{
    public BillDraft? Current { get; private set; }

    public BillDraft Start()
    {
        Current = new BillDraft();
        return Current;
    }

    // Load an existing bill into an editable draft (used by the Edit action).
    public BillDraft StartEdit(BillDetail detail)
    {
        Current = new BillDraft
        {
            BillId = detail.Bill.Id,
            CreatedDate = detail.Bill.CreatedDate,
            Date = detail.Bill.Date,
            IsDone = detail.Bill.IsDone,
            Name = detail.Bill.Name,
            CategoryId = detail.Bill.CategoryId == 0 ? null : detail.Bill.CategoryId,
            PayerId = detail.Bill.PayerId,
            PeopleIds = detail.People.Select(p => p.Id).ToList(),
            TotalText = detail.Bill.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Items = detail.Items.Select(it => new DraftItem
            {
                Name = it.Name,
                Price = it.Price,
                Excluded = detail.ExclusionsByItem.TryGetValue(it.Id, out var ex)
                    ? new HashSet<int>(ex) : new HashSet<int>(),
            }).ToList(),
        };
        return Current;
    }

    public void Clear() => Current = null;
}
