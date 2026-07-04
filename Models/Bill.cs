using SQLite;

namespace SplitBillApp.Models;

// A single bill to split. Either a flat total or an itemized list of menu items.
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

    // true  => a single "Total" item split equally among included people
    // false => an itemized bill, each item split among its included people
    public bool IsFlat { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Display-only, filled in when listing.
    [Ignore] public string PayerName { get; set; } = string.Empty;
    [Ignore] public string CategoryName { get; set; } = string.Empty;
    [Ignore] public decimal Total { get; set; }
    [Ignore] public List<Person> People { get; set; } = new();
}
