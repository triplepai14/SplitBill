using SQLite;
using SplitBillApp.Models;

namespace SplitBillApp.Data;

/// <summary>
/// Offline-first data layer. Everything lives in a local SQLite file inside
/// the app's private data directory. No network is required at any point —
/// the app is fully functional offline. (If you later add cloud sync, this is
/// the single place you'd add a "dirty/needs-sync" flag and a push/pull step.)
/// </summary>
public class DatabaseService
{
    private SQLiteAsyncConnection? _db;

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_db is not null) return _db;

        var path = Path.Combine(FileSystem.AppDataDirectory, "splitbill.db3");
        _db = new SQLiteAsyncConnection(
            path,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        await _db.CreateTableAsync<Group>();
        await _db.CreateTableAsync<Person>();
        await _db.CreateTableAsync<Expense>();
        await _db.CreateTableAsync<ExpenseSplit>();
        return _db;
    }

    // ---------- Groups ----------
    public async Task<List<Group>> GetGroupsAsync()
    {
        var db = await GetConnectionAsync();
        var groups = await db.Table<Group>().OrderByDescending(g => g.CreatedDate).ToListAsync();

        // Enrich with member count + total spent for the list display
        foreach (var g in groups)
        {
            g.MemberCount = await db.Table<Person>().Where(p => p.GroupId == g.Id).CountAsync();
            var expenses = await db.Table<Expense>().Where(e => e.GroupId == g.Id).ToListAsync();
            g.TotalSpent = expenses.Sum(e => e.Amount);
        }
        return groups;
    }

    public async Task<Group> GetGroupAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.GetAsync<Group>(id);
    }

    public async Task<int> SaveGroupAsync(Group group)
    {
        var db = await GetConnectionAsync();
        if (group.Id != 0)
        {
            await db.UpdateAsync(group);
            return group.Id;
        }
        await db.InsertAsync(group);
        return group.Id; // sqlite-net populates the PK on insert
    }

    public async Task DeleteGroupAsync(int groupId)
    {
        var db = await GetConnectionAsync();
        // Cascade delete: expenses -> their splits, then people, then the group
        var expenses = await db.Table<Expense>().Where(e => e.GroupId == groupId).ToListAsync();
        foreach (var e in expenses)
            await db.Table<ExpenseSplit>().Where(s => s.ExpenseId == e.Id).DeleteAsync();

        await db.Table<Expense>().Where(e => e.GroupId == groupId).DeleteAsync();
        await db.Table<Person>().Where(p => p.GroupId == groupId).DeleteAsync();
        await db.DeleteAsync<Group>(groupId);
    }

    // ---------- People ----------
    public async Task<List<Person>> GetPeopleAsync(int groupId)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Person>().Where(p => p.GroupId == groupId).ToListAsync();
    }

    public async Task<int> SavePersonAsync(Person person)
    {
        var db = await GetConnectionAsync();
        if (person.Id != 0) { await db.UpdateAsync(person); return person.Id; }
        await db.InsertAsync(person);
        return person.Id;
    }

    public async Task DeletePersonAsync(int personId)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAsync<Person>(personId);
    }

    // ---------- Expenses + Splits ----------
    public async Task<List<Expense>> GetExpensesAsync(int groupId)
    {
        var db = await GetConnectionAsync();
        var expenses = await db.Table<Expense>()
            .Where(e => e.GroupId == groupId)
            .OrderByDescending(e => e.Date)
            .ToListAsync();

        var people = await GetPeopleAsync(groupId);
        foreach (var e in expenses)
            e.PaidByName = people.FirstOrDefault(p => p.Id == e.PaidByPersonId)?.Name ?? "Unknown";

        return expenses;
    }

    public async Task<List<ExpenseSplit>> GetSplitsAsync(int expenseId)
    {
        var db = await GetConnectionAsync();
        return await db.Table<ExpenseSplit>().Where(s => s.ExpenseId == expenseId).ToListAsync();
    }

    /// <summary>
    /// Saves an expense together with one split row per participant.
    /// participantIds = the people who share this cost (split equally).
    /// </summary>
    public async Task SaveExpenseAsync(Expense expense, List<int> participantIds)
    {
        var db = await GetConnectionAsync();

        if (expense.Id != 0)
        {
            await db.UpdateAsync(expense);
            await db.Table<ExpenseSplit>().Where(s => s.ExpenseId == expense.Id).DeleteAsync();
        }
        else
        {
            await db.InsertAsync(expense);
        }

        if (participantIds.Count == 0) return;

        // Split equally, pushing rounding remainder onto the first participant
        // so the splits always sum exactly to the expense amount.
        var share = Math.Round(expense.Amount / participantIds.Count, 2);
        var splits = participantIds
            .Select(pid => new ExpenseSplit { ExpenseId = expense.Id, PersonId = pid, Amount = share })
            .ToList();

        var remainder = expense.Amount - share * participantIds.Count;
        splits[0].Amount += remainder;

        await db.InsertAllAsync(splits);
    }

    public async Task DeleteExpenseAsync(int expenseId)
    {
        var db = await GetConnectionAsync();
        await db.Table<ExpenseSplit>().Where(s => s.ExpenseId == expenseId).DeleteAsync();
        await db.DeleteAsync<Expense>(expenseId);
    }

    /// <summary>
    /// Net balance per person for a group:
    ///   positive => the group owes them (they overpaid)
    ///   negative => they owe the group
    /// balance = (total they paid) - (total of their shares)
    /// </summary>
    public async Task<Dictionary<int, decimal>> GetNetBalancesAsync(int groupId)
    {
        var db = await GetConnectionAsync();
        var people = await GetPeopleAsync(groupId);
        var balances = people.ToDictionary(p => p.Id, _ => 0m);

        var expenses = await db.Table<Expense>().Where(e => e.GroupId == groupId).ToListAsync();
        foreach (var e in expenses)
        {
            if (balances.ContainsKey(e.PaidByPersonId))
                balances[e.PaidByPersonId] += e.Amount;

            var splits = await GetSplitsAsync(e.Id);
            foreach (var s in splits)
                if (balances.ContainsKey(s.PersonId))
                    balances[s.PersonId] -= s.Amount;
        }
        return balances;
    }
}
