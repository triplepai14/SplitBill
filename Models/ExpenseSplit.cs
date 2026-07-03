using SQLite;

namespace SplitBillApp.Models;

// One row per person-share of a given expense.
// e.g. a 900 THB dinner split among 3 people => 3 rows of 300 each.
[Table("expense_splits")]
public class ExpenseSplit
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed] public int ExpenseId { get; set; }

    public int PersonId { get; set; }

    public decimal Amount { get; set; }
}
