using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SplitBillApp.Data;
using SplitBillApp.Models;
using SplitBillApp.Services;
using SplitBillApp.Views;

namespace SplitBillApp.ViewModels;

[QueryProperty(nameof(GroupId), "groupId")]
public partial class GroupDetailViewModel : BaseViewModel
{
    private readonly DatabaseService _db;

    [ObservableProperty] private int groupId;
    [ObservableProperty] private string newPersonName = string.Empty;

    public ObservableCollection<Person> People { get; } = new();
    public ObservableCollection<Expense> Expenses { get; } = new();
    public ObservableCollection<Settlement> Settlements { get; } = new();

    public GroupDetailViewModel(DatabaseService db) => _db = db;

    partial void OnGroupIdChanged(int value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || GroupId == 0) return;
        try
        {
            IsBusy = true;

            var group = await _db.GetGroupAsync(GroupId);
            Title = group.Name;

            People.Clear();
            foreach (var p in await _db.GetPeopleAsync(GroupId)) People.Add(p);

            Expenses.Clear();
            foreach (var e in await _db.GetExpensesAsync(GroupId)) Expenses.Add(e);

            await RefreshSettlementsAsync();
        }
        finally { IsBusy = false; }
    }

    private async Task RefreshSettlementsAsync()
    {
        var names = People.ToDictionary(p => p.Id, p => p.Name);
        var balances = await _db.GetNetBalancesAsync(GroupId);

        Settlements.Clear();
        foreach (var s in SettlementCalculator.Calculate(names, balances))
            Settlements.Add(s);
    }

    [RelayCommand]
    private async Task AddPersonAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPersonName)) return;
        await _db.SavePersonAsync(new Person { GroupId = GroupId, Name = NewPersonName.Trim() });
        NewPersonName = string.Empty;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeletePersonAsync(Person person)
    {
        if (person is null) return;
        await _db.DeletePersonAsync(person.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddExpenseAsync()
    {
        if (People.Count == 0)
        {
            await Shell.Current.DisplayAlert("Add people first",
                "You need at least one person in the group before adding an expense.", "OK");
            return;
        }
        await Shell.Current.GoToAsync($"{nameof(AddExpensePage)}?groupId={GroupId}");
    }

    [RelayCommand]
    private async Task DeleteExpenseAsync(Expense expense)
    {
        if (expense is null) return;
        await _db.DeleteExpenseAsync(expense.Id);
        await LoadAsync();
    }
}
