using SQLite;

namespace SplitBillApp.Models;

// One line on a bill. A flat bill has exactly one item named "Total".
[Table("menu_items")]
public class BillItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed] public int BillId { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
