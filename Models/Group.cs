using SQLite;

namespace SplitBillApp.Models;

[Table("groups")]
public class Group
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Not stored in DB — populated for display only
    [Ignore] public int MemberCount { get; set; }
    [Ignore] public decimal TotalSpent { get; set; }
}
