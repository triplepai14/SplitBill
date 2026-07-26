using SQLite;

namespace SplitBillApp.Models;

// A grouping for bills, e.g. "Chiang Mai Trip" or "Office Lunch".
[Table("categories")]
public class Category
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // The trip/event date (defaults to now, user-editable).
    public DateTime Date { get; set; } = DateTime.Now;

    // Done = the whole trip is settled; moves it (and its bills) to the Done tab.
    public bool IsDone { get; set; }

    // Display-only aggregates, filled in when listing categories.
    [Ignore] public int BillCount { get; set; }
    [Ignore] public decimal Total { get; set; }
    [Ignore] public List<Person> People { get; set; } = new();
}
