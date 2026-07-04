using SQLite;

namespace SplitBillApp.Models;

// People are a global roster (your "friends"), reused across every bill.
[Table("people")]
public class Person
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // Stable index into the avatar palette so a person keeps the same colour
    // everywhere they appear.
    public int ColorIndex { get; set; }
}
