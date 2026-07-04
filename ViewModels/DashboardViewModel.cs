using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitnessQuest.Data;
using FitnessQuest.Models;
using FitnessQuest.Services;
using FitnessQuest.Views;

namespace FitnessQuest.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly AppDatabase _db;
    private readonly ChallengeService _challenges;
    private readonly FeedbackService _feedback;

    public DashboardViewModel(AppDatabase db, ChallengeService challenges, FeedbackService feedback)
    {
        _db = db;
        _challenges = challenges;
        _feedback = feedback;
        Title = "FitnessQuest";
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this, (_, _) => _ = LoadAsync());
    }

    public ObservableCollection<DailyChallenge> Challenges { get; } = new();

    [ObservableProperty] private int _waterGlasses;
    [ObservableProperty] private int _waterGoal = 8;
    [ObservableProperty] private double _waterProgress;
    [ObservableProperty] private string _waterLabel = "0 / 8 glas";

    // Player card
    [ObservableProperty] private int _level = 1;
    [ObservableProperty] private string _rank = "Nybörjare";
    [ObservableProperty] private int _totalXp;
    [ObservableProperty] private double _levelProgress;
    [ObservableProperty] private string _xpLabel = "0 / 100 XP";
    [ObservableProperty] private int _currentStreak;
    [ObservableProperty] private int _longestStreak;
    [ObservableProperty] private string _greeting = "Välkommen!";

    // Nutrition today
    [ObservableProperty] private double _calories;
    [ObservableProperty] private int _calorieGoal = 2200;
    [ObservableProperty] private double _calorieProgress;
    [ObservableProperty] private string _calorieLabel = "0 / 2200 kcal";
    [ObservableProperty] private double _protein;
    [ObservableProperty] private int _proteinGoal = 150;
    [ObservableProperty] private double _proteinProgress;
    [ObservableProperty] private double _carbs;
    [ObservableProperty] private int _carbsGoal = 220;
    [ObservableProperty] private double _carbsProgress;
    [ObservableProperty] private double _fat;
    [ObservableProperty] private int _fatGoal = 70;
    [ObservableProperty] private double _fatProgress;

    // Training today
    [ObservableProperty] private int _workoutsToday;
    [ObservableProperty] private double _cardioKmToday;
    [ObservableProperty] private int _unlockedCount;
    [ObservableProperty] private int _totalAchievements;

    /// <summary>App version shown at the bottom of the dashboard.</summary>
    public string AppVersion => $"FitnessQuest v{AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";

    [RelayCommand]
    private async Task Load() => await LoadAsync();

    private async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;

            var state = await _db.GetStateAsync();
            var profile = await _db.GetProfileAsync();

            Level = state.Level;
            Rank = state.Rank;
            TotalXp = state.TotalXp;
            LevelProgress = state.LevelProgress;
            XpLabel = $"{state.XpIntoLevel} / {state.XpForNextLevel} XP";
            CurrentStreak = state.CurrentStreak;
            LongestStreak = state.LongestStreak;
            Greeting = state.CurrentStreak > 0
                ? $"Hej {profile.Name}! 🔥 {state.CurrentStreak} dagar i rad"
                : $"Hej {profile.Name}! Dags att logga något?";

            // Nutrition rollup for today
            var todayFood = await _db.GetFoodLogForDayAsync(DateTime.Today);
            Calories = todayFood.Sum(e => e.Kcal);
            Protein = todayFood.Sum(e => e.Protein);
            Carbs = todayFood.Sum(e => e.Carbs);
            Fat = todayFood.Sum(e => e.Fat);

            CalorieGoal = profile.CalorieGoal;
            ProteinGoal = profile.ProteinGoal;
            CarbsGoal = profile.CarbsGoal;
            FatGoal = profile.FatGoal;

            CalorieProgress = Clamp(Calories / Math.Max(1, CalorieGoal));
            ProteinProgress = Clamp(Protein / Math.Max(1, ProteinGoal));
            CarbsProgress = Clamp(Carbs / Math.Max(1, CarbsGoal));
            FatProgress = Clamp(Fat / Math.Max(1, FatGoal));
            CalorieLabel = $"{Calories:0} / {CalorieGoal} kcal";

            // Training today
            var workouts = await _db.GetRecentWorkoutsAsync();
            WorkoutsToday = workouts.Count(w => w.PerformedAt.Date == DateTime.Today);
            var cardio = await _db.GetRecentCardioAsync();
            CardioKmToday = cardio.Where(c => c.PerformedAt.Date == DateTime.Today).Sum(c => c.DistanceKm);

            var achievements = await _db.GetAchievementsAsync();
            TotalAchievements = achievements.Count;
            UnlockedCount = achievements.Count(a => a.IsUnlocked);

            // Water
            WaterGoal = profile.WaterGoalGlasses;
            WaterGlasses = await _db.GetWaterAsync(DateTime.Today);
            UpdateWater();

            // Daily challenges (also awards XP for newly completed)
            var challengeResult = await _challenges.EnsureAndEvaluateAsync();
            Challenges.Clear();
            foreach (var c in challengeResult.Challenges)
                Challenges.Add(c);
            if (challengeResult.Reward is { } r && (r.LeveledUp || r.NewAchievements.Count > 0 || r.XpGained > 0))
                await _feedback.CelebrateAsync(r);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateWater()
    {
        WaterProgress = WaterGoal <= 0 ? 0 : Math.Min(1.0, (double)WaterGlasses / WaterGoal);
        WaterLabel = $"{WaterGlasses} / {WaterGoal} glas";
    }

    [RelayCommand]
    private async Task AddWater()
    {
        WaterGlasses++;
        UpdateWater();
        await _db.SetWaterAsync(DateTime.Today, WaterGlasses);
        WeakReferenceMessenger.Default.Send(new DataChangedMessage("water"));
    }

    [RelayCommand]
    private async Task RemoveWater()
    {
        if (WaterGlasses <= 0) return;
        WaterGlasses--;
        UpdateWater();
        await _db.SetWaterAsync(DateTime.Today, WaterGlasses);
        WeakReferenceMessenger.Default.Send(new DataChangedMessage("water"));
    }

    private static double Clamp(double v) => Math.Max(0, Math.Min(1, v));

    [RelayCommand] private Task GoNutrition() => Shell.Current.GoToAsync("//nutrition");
    [RelayCommand] private Task GoGym() => Shell.Current.GoToAsync("//gym");
    [RelayCommand] private Task GoCardio() => Shell.Current.GoToAsync("//cardio");
    [RelayCommand] private Task GoAchievements() => Shell.Current.GoToAsync(nameof(AchievementsPage));
    [RelayCommand] private Task GoProfile() => Shell.Current.GoToAsync(nameof(ProfilePage));
}
