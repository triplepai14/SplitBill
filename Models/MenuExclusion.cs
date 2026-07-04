using SQLite;

namespace SplitBillApp.Models;

// Marks a person as NOT sharing a particular menu item (e.g. didn't drink the
// beer tower). Absence of a row means the person is included in that item.
[Table("menu_exclusions")]
public class MenuExclusion
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed] public int MenuItemId { get; set; }
    public int PersonId { get; set; }
}
