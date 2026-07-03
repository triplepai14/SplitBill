using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SplitBillApp.Data;
using SplitBillApp.Models;
using SplitBillApp.Views;

namespace SplitBillApp.ViewModels;

public partial class GroupsViewModel : BaseViewModel
{
    private readonly DatabaseService _db;

    public ObservableCollection<Group> Groups { get; } = new();

    [ObservableProperty] private string newGroupName = string.Empty;

    public GroupsViewModel(DatabaseService db)
    {
        _db = db;
        Title = "My Groups";
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            Groups.Clear();
            foreach (var g in await _db.GetGroupsAsync())
                Groups.Add(g);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddGroupAsync()
    {
        if (string.IsNullOrWhiteSpace(NewGroupName)) return;
        await _db.SaveGroupAsync(new Group { Name = NewGroupName.Trim() });
        NewGroupName = string.Empty;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteGroupAsync(Group group)
    {
        if (group is null) return;
        await _db.DeleteGroupAsync(group.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task OpenGroupAsync(Group group)
    {
        if (group is null) return;
        await Shell.Current.GoToAsync($"{nameof(GroupDetailPage)}?groupId={group.Id}");
    }
}
