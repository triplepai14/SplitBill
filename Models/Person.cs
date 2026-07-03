using SQLite;

namespace SplitBillApp.Models;

[Table("people")]
public class Person
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed] public int GroupId { get; set; }

    public string Name { get; set; } = string.Empty;
}
