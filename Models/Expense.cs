using SQLite;

namespace SplitBillApp.Models;

[Table("expenses")]
public class Expense
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed] public int GroupId { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    // The person who actually paid the bill
    public int PaidByPersonId { get; set; }

    public DateTime Date { get; set; } = DateTime.Now;

    // For display only
    [Ignore] public string PaidByName { get; set; } = string.Empty;
}
