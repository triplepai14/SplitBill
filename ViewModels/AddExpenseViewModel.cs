using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SplitBillApp.Data;
using SplitBillApp.Models;

namespace SplitBillApp.ViewModels;

// A small wrapper so each person gets a checkbox in the "split between" list.
public partial class Participant : ObservableObject
{
    public int PersonId { get; set; }
    public string Name { get; set; } = string.Empty;
    [ObservableProperty] private bool isSelected = true;
}

[QueryProperty(nameof(GroupId), "groupId")]
public partial class AddExpenseViewModel : BaseViewModel
{
    private readonly DatabaseService _db;

    [ObservableProperty] private int groupId;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private string amountText = string.Empty;
    [ObservableProperty] private Person? paidBy;

    public ObservableCollection<Person> People { get; } = new();
    public ObservableCollection<Participant> Participants { get; } = new();

    public AddExpenseViewModel(DatabaseService db)
    {
        _db = db;
        Title = "Add Expense";
    }

    partial void OnGroupIdChanged(int value) => _ = LoadAsync();

    private async Task LoadAsync()
    {
        if (GroupId == 0) return;
        var people = await _db.GetPeopleAsync(GroupId);

        People.Clear();
        Participants.Clear();
        foreach (var p in people)
        {
            People.Add(p);
            Participants.Add(new Participant { PersonId = p.Id, Name = p.Name, IsSelected = true });
        }
        PaidBy = People.FirstOrDefault();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!decimal.TryParse(AmountText, out var amount) || amount <= 0)
        {
            await Shell.Current.DisplayAlert("Invalid amount", "Enter an amount greater than 0.", "OK");
            return;
        }
        if (PaidBy is null)
        {
            await Shell.Current.DisplayAlert("Who paid?", "Select who paid the bill.", "OK");
            return;
        }

        var participantIds = Participants.Where(p => p.IsSelected).Select(p => p.PersonId).ToList();
        if (participantIds.Count == 0)
        {
            await Shell.Current.DisplayAlert("Split with whom?", "Select at least one person to split with.", "OK");
            return;
        }

        var expense = new Expense
        {
            GroupId = GroupId,
            Description = string.IsNullOrWhiteSpace(Description) ? "Expense" : Description.Trim(),
            Amount = amount,
            PaidByPersonId = PaidBy.Id,
            Date = DateTime.Now
        };

        await _db.SaveExpenseAsync(expense, participantIds);
        await Shell.Current.GoToAsync(".."); // back to group detail
    }

    [RelayCommand]
    private async Task CancelAsync() => await Shell.Current.GoToAsync("..");
}
