using SQLite;

namespace SplitBillApp.Models;

// A single bill to split: an authoritative total amount, plus optional items
// that some people didn't share (carved out of the total; the rest splits
// equally). See BillMath.Shares.
[Table("bills")]
public class Bill
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // 0 = uncategorized
    public int CategoryId { get; set; }

    // The person who fronted the money for the whole bill.
    public int PayerId { get; set; }

    // The total bill amount, entered by the user (not an auto-sum of items).
    public decimal Amount { get; set; }

    // The bill's own date (defaults to now, user-editable).
    public DateTime Date { get; set; } = DateTime.Now;

    // Only meaningful for uncategorized bills — a bill inside a category
    // follows that category's Done state instead.
    public bool IsDone { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Bumped on every save; equals CreatedDate for a bill never edited.
    public DateTime ModifiedDate { get; set; } = DateTime.Now;

    // Display-only, filled in when listing.
    [Ignore] public string PayerName { get; set; } = string.Empty;
    [Ignore] public string CategoryName { get; set; } = string.Empty;
    [Ignore] public decimal Total { get; set; }
    [Ignore] public List<Person> People { get; set; } = new();
}
