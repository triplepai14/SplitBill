using SQLite;

namespace SplitBillApp.Models;

// A grouping for bills, e.g. "Chiang Mai Trip" or "Office Lunch".
[Table("categories")]
public class Category
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // Display-only aggregates, filled in when listing categories.
    [Ignore] public int BillCount { get; set; }
    [Ignore] public decimal Total { get; set; }
}
