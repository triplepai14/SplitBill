using SQLite;

namespace SplitBillApp.Models;

// Join row: which people are splitting a given bill.
[Table("bill_people")]
public class BillPerson
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed] public int BillId { get; set; }
    public int PersonId { get; set; }
}
