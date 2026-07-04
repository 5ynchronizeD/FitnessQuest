using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitnessQuest.Data;
using FitnessQuest.Models;
using FitnessQuest.Services;

namespace FitnessQuest.ViewModels;

public partial class AchievementsViewModel : BaseViewModel
{
    private readonly AppDatabase _db;

    public AchievementsViewModel(AppDatabase db)
    {
        _db = db;
        Title = "Bragder";
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this, (_, _) => _ = LoadAsync());
    }

    public ObservableCollection<Achievement> Achievements { get; } = new();

    [ObservableProperty] private int _level = 1;
    [ObservableProperty] private string _rank = "Nybörjare";
    [ObservableProperty] private int _totalXp;
    [ObservableProperty] private int _currentStreak;
    [ObservableProperty] private int _longestStreak;
    [ObservableProperty] private int _unlockedCount;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private string _progressLabel = "0 / 0 upplåsta";

    [RelayCommand]
    private async Task Load() => await LoadAsync();

    private async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var state = await _db.GetStateAsync();
            Level = state.Level;
            Rank = state.Rank;
            TotalXp = state.TotalXp;
            CurrentStreak = state.CurrentStreak;
            LongestStreak = state.LongestStreak;

            var list = await _db.GetAchievementsAsync();
            Achievements.Clear();
            // Unlocked first, then locked, each keeping catalogue order.
            foreach (var a in list.OrderByDescending(a => a.IsUnlocked).ThenBy(a => a.SortOrder))
                Achievements.Add(a);

            UnlockedCount = list.Count(a => a.IsUnlocked);
            TotalCount = list.Count;
            ProgressLabel = $"{UnlockedCount} / {TotalCount} upplåsta";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
