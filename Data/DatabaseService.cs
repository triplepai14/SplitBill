using SQLite;
using SplitBillApp.Models;
using SplitBillApp.Services;

namespace SplitBillApp.Data;

/// <summary>
/// Rich, fully-loaded view of one bill: its people, items and per-item
/// exclusions, plus the computed share for every participant.
/// </summary>
public class BillDetail
{
    public Bill Bill { get; set; } = new();
    public List<Person> People { get; set; } = new();
    public List<BillItem> Items { get; set; } = new();
    public Dictionary<int, HashSet<int>> ExclusionsByItem { get; set; } = new();
    public Dictionary<int, decimal> Shares { get; set; } = new();
    public decimal Total { get; set; }
}

/// <summary>
/// Offline-first data layer. Everything lives in a local SQLite file inside the
/// app's private data directory — no network is ever required.
/// </summary>
public class DatabaseService
{
    private SQLiteAsyncConnection? _db;

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_db is not null) return _db;

        var path = Path.Combine(FileSystem.AppDataDirectory, "splitbill_v2.db3");
        _db = new SQLiteAsyncConnection(
            path,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        await _db.CreateTableAsync<Category>();
        await _db.CreateTableAsync<Person>();
        await _db.CreateTableAsync<Bill>();
        await _db.CreateTableAsync<BillPerson>();
        await _db.CreateTableAsync<BillItem>();
        await _db.CreateTableAsync<MenuExclusion>();

        await SeedAsync(_db);
        return _db;
    }

    // ---------- People ----------
    public async Task<List<Person>> GetPeopleAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<Person>().OrderBy(p => p.Id).ToListAsync();
    }

    public async Task<Person> SavePersonAsync(string name)
    {
        var db = await GetConnectionAsync();
        var count = await db.Table<Person>().CountAsync();
        var person = new Person { Name = name.Trim(), ColorIndex = count };
        await db.InsertAsync(person);
        return person;
    }

    // ---------- Categories ----------
    public async Task<List<Category>> GetCategoriesAsync()
    {
        var db = await GetConnectionAsync();
        var cats = await db.Table<Category>().OrderBy(c => c.Id).ToListAsync();
        foreach (var c in cats)
        {
            var bills = await db.Table<Bill>().Where(b => b.CategoryId == c.Id).ToListAsync();
            c.BillCount = bills.Count;
            c.Total = bills.Sum(b => b.Amount);
        }
        return cats;
    }

    public async Task<Category?> GetCategoryAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.FindAsync<Category>(id);
    }

    public async Task<Category> SaveCategoryAsync(string name)
    {
        var db = await GetConnectionAsync();
        var cat = new Category { Name = name.Trim() };
        await db.InsertAsync(cat);
        return cat;
    }

    // ---------- Bills ----------
    public async Task<List<Bill>> GetBillsAsync(int? categoryId = null)
    {
        var db = await GetConnectionAsync();
        var query = db.Table<Bill>();
        var bills = await query.OrderByDescending(b => b.CreatedDate).ToListAsync();
        if (categoryId is int cid)
            bills = bills.Where(b => b.CategoryId == cid).ToList();

        var people = await GetPeopleAsync();
        var cats = await db.Table<Category>().ToListAsync();

        foreach (var b in bills)
        {
            b.PayerName = people.FirstOrDefault(p => p.Id == b.PayerId)?.Name ?? "?";
            b.CategoryName = cats.FirstOrDefault(c => c.Id == b.CategoryId)?.Name ?? "";
            b.Total = b.Amount;
            var ids = (await db.Table<BillPerson>().Where(bp => bp.BillId == b.Id).ToListAsync())
                .Select(bp => bp.PersonId).ToList();
            b.People = people.Where(p => ids.Contains(p.Id)).ToList();
        }
        return bills;
    }

    public async Task DeleteBillAsync(int billId)
    {
        var db = await GetConnectionAsync();
        var items = await db.Table<BillItem>().Where(m => m.BillId == billId).ToListAsync();
        foreach (var it in items)
            await db.Table<MenuExclusion>().Where(x => x.MenuItemId == it.Id).DeleteAsync();
        await db.Table<BillItem>().Where(m => m.BillId == billId).DeleteAsync();
        await db.Table<BillPerson>().Where(bp => bp.BillId == billId).DeleteAsync();
        await db.DeleteAsync<Bill>(billId);
    }

    /// <summary>
    /// Persists a bill: the bill row, its participants, its items and each
    /// item's exclusions. Inserts when <c>bill.Id == 0</c>, otherwise replaces
    /// the existing bill's children in place. Returns the bill id.
    /// </summary>
    public async Task<int> SaveBillAsync(
        Bill bill,
        List<int> peopleIds,
        List<(string Name, decimal Price, List<int> ExcludedPeopleIds)> items)
    {
        var db = await GetConnectionAsync();
        bill.ModifiedDate = DateTime.Now;

        if (bill.Id == 0)
        {
            bill.CreatedDate = bill.ModifiedDate;   // a new bill is created "now"
            await db.InsertAsync(bill);
        }
        else
        {
            await db.UpdateAsync(bill);
            // Clear the old children before re-inserting the edited set.
            var oldItems = await db.Table<BillItem>().Where(m => m.BillId == bill.Id).ToListAsync();
            foreach (var it in oldItems)
                await db.Table<MenuExclusion>().Where(x => x.MenuItemId == it.Id).DeleteAsync();
            await db.Table<BillItem>().Where(m => m.BillId == bill.Id).DeleteAsync();
            await db.Table<BillPerson>().Where(bp => bp.BillId == bill.Id).DeleteAsync();
        }

        foreach (var pid in peopleIds)
            await db.InsertAsync(new BillPerson { BillId = bill.Id, PersonId = pid });

        foreach (var (name, price, excluded) in items)
        {
            var mi = new BillItem { BillId = bill.Id, Name = name, Price = price };
            await db.InsertAsync(mi);
            foreach (var pid in excluded)
                await db.InsertAsync(new MenuExclusion { MenuItemId = mi.Id, PersonId = pid });
        }
        return bill.Id;
    }

    public async Task<BillDetail?> GetBillDetailAsync(int billId)
    {
        var db = await GetConnectionAsync();
        var bill = await db.FindAsync<Bill>(billId);
        if (bill is null) return null;

        var allPeople = await GetPeopleAsync();
        var cats = await db.Table<Category>().ToListAsync();
        bill.PayerName = allPeople.FirstOrDefault(p => p.Id == bill.PayerId)?.Name ?? "?";
        bill.CategoryName = cats.FirstOrDefault(c => c.Id == bill.CategoryId)?.Name ?? "";

        var ids = (await db.Table<BillPerson>().Where(bp => bp.BillId == billId).ToListAsync())
            .Select(bp => bp.PersonId).ToList();
        var people = allPeople.Where(p => ids.Contains(p.Id)).ToList();

        var items = await db.Table<BillItem>().Where(m => m.BillId == billId).ToListAsync();
        var exMap = new Dictionary<int, HashSet<int>>();
        foreach (var it in items)
        {
            var ex = await db.Table<MenuExclusion>().Where(x => x.MenuItemId == it.Id).ToListAsync();
            exMap[it.Id] = ex.Select(e => e.PersonId).ToHashSet();
        }

        var shares = BillMath.Shares(ids, bill.Amount, items, exMap);
        return new BillDetail
        {
            Bill = bill,
            People = people,
            Items = items,
            ExclusionsByItem = exMap,
            Shares = shares,
            Total = bill.Amount
        };
    }

    // ---------- Seed (first run only) ----------
    private static async Task SeedAsync(SQLiteAsyncConnection db)
    {
        if (await db.Table<Category>().CountAsync() > 0) return;

        var friendNames = new[] { "Bua", "Nong", "Ploy", "Tim", "Mind", "Beam" };
        var friends = new List<Person>();
        for (int i = 0; i < friendNames.Length; i++)
        {
            var p = new Person { Name = friendNames[i], ColorIndex = i };
            await db.InsertAsync(p);
            friends.Add(p);
        }
        int F(string name) => friends.First(f => f.Name == name).Id;

        var trip = new Category { Name = "Chiang Mai Trip" };
        var office = new Category { Name = "Office Lunch" };
        await db.InsertAsync(trip);
        await db.InsertAsync(office);

        async Task Seed(string name, int catId, string payer, string[] people,
            (string n, decimal p, string[] excl)[] items)
        {
            // Seed bills are fully itemized, so the total equals the item sum
            // (the "base" split is zero) — the exclusions still demo correctly.
            var bill = new Bill
            {
                Name = name, CategoryId = catId, PayerId = F(payer),
                Amount = items.Sum(i => i.p),
            };
            await db.InsertAsync(bill);
            foreach (var pn in people)
                await db.InsertAsync(new BillPerson { BillId = bill.Id, PersonId = F(pn) });
            foreach (var (n, price, excl) in items)
            {
                var mi = new BillItem { BillId = bill.Id, Name = n, Price = price };
                await db.InsertAsync(mi);
                foreach (var e in excl)
                    await db.InsertAsync(new MenuExclusion { MenuItemId = mi.Id, PersonId = F(e) });
            }
        }

        await Seed("Som Tam Dinner", trip.Id, "Bua",
            new[] { "Bua", "Nong", "Ploy", "Tim" },
            new (string, decimal, string[])[]
            {
                ("Som Tam", 120, Array.Empty<string>()),
                ("Grilled Chicken", 180, Array.Empty<string>()),
                ("Beer Tower", 240, new[] { "Ploy" }),
                ("Sticky Rice", 40, Array.Empty<string>()),
            });

        await Seed("Coffee Run", office.Id, "Mind",
            new[] { "Mind", "Beam", "Ploy" },
            new (string, decimal, string[])[]
            {
                ("Latte", 85, Array.Empty<string>()),
                ("Americano", 70, Array.Empty<string>()),
                ("Croissant", 95, new[] { "Beam" }),
            });
    }
}
