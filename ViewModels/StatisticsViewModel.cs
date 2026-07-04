using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitnessQuest.Data;
using FitnessQuest.Services;

namespace FitnessQuest.ViewModels;

public partial class StatisticsViewModel : BaseViewModel
{
    private readonly AppDatabase _db;
    private readonly StatsService _stats;

    public StatisticsViewModel(AppDatabase db, StatsService stats)
    {
        _db = db;
        _stats = stats;
        Title = "Statistik";
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this, (_, _) => _ = LoadAsync());
    }

    /// <summary>Raised when chart data changes so the view can invalidate its GraphicsViews.</summary>
    public event Action? ChartsUpdated;

    // Nutrition
    public double[] CalorieValues { get; private set; } = Array.Empty<double>();
    public string[] CalorieLabels { get; private set; } = Array.Empty<string>();
    public double CalorieGoal { get; private set; } = 2200;
    [ObservableProperty] private double _avgKcal;
    [ObservableProperty] private double _avgProtein;

    // Training
    public double[] VolumeValues { get; private set; } = Array.Empty<double>();
    public string[] VolumeLabels { get; private set; } = Array.Empty<string>();
    [ObservableProperty] private double _avgWeeklyVolume;
    [ObservableProperty] private double _workoutsPerWeek;

    // Cardio
    public double[] CardioValues { get; private set; } = Array.Empty<double>();
    public string[] CardioLabels { get; private set; } = Array.Empty<string>();
    [ObservableProperty] private double _cardioTotal14;

    // Progression
    public double[] ProgressionValues { get; private set; } = Array.Empty<double>();
    public string[] ProgressionLabels { get; private set; } = Array.Empty<string>();
    public ObservableCollection<string> ExerciseNames { get; } = new();
    [ObservableProperty] private string? _selectedExercise;
    [ObservableProperty] private bool _hasProgression;

    [RelayCommand]
    private async Task Load() => await LoadAsync();

    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var profile = await _db.GetProfileAsync();
            CalorieGoal = profile.CalorieGoal;

            var nutrition = await _stats.GetNutritionDailyAsync(7);
            CalorieValues = nutrition.Select(d => Math.Round(d.Kcal)).ToArray();
            CalorieLabels = nutrition.Select(d => WeekdayLetter(d.Date)).ToArray();
            AvgKcal = nutrition.Count > 0 ? nutrition.Average(d => d.Kcal) : 0;
            AvgProtein = nutrition.Count > 0 ? nutrition.Average(d => d.Protein) : 0;

            var weekly = await _stats.GetTrainingWeeklyAsync(8);
            VolumeValues = weekly.Select(w => Math.Round(w.VolumeKg)).ToArray();
            VolumeLabels = weekly.Select(w => w.WeekStart.ToString("d/M")).ToArray();
            AvgWeeklyVolume = weekly.Count > 0 ? weekly.Average(w => w.VolumeKg) : 0;
            WorkoutsPerWeek = weekly.Count > 0 ? weekly.Average(w => w.Workouts) : 0;

            var cardio = await _stats.GetCardioDailyAsync(14);
            CardioValues = cardio.Select(c => Math.Round(c.Value, 1)).ToArray();
            CardioLabels = cardio.Select((c, i) => i % 2 == 0 ? c.Date.ToString("d/M") : string.Empty).ToArray();
            CardioTotal14 = cardio.Sum(c => c.Value);

            var names = await _stats.GetTrainedExerciseNamesAsync();
            ExerciseNames.Clear();
            foreach (var n in names) ExerciseNames.Add(n);
            SelectedExercise ??= ExerciseNames.FirstOrDefault();

            await LoadProgressionAsync();

            ChartsUpdated?.Invoke();
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedExerciseChanged(string? value) => _ = LoadProgressionAsync(invalidate: true);

    private async Task LoadProgressionAsync(bool invalidate = false)
    {
        if (string.IsNullOrWhiteSpace(SelectedExercise))
        {
            ProgressionValues = Array.Empty<double>();
            ProgressionLabels = Array.Empty<string>();
            HasProgression = false;
        }
        else
        {
            var points = await _stats.GetExerciseProgressionAsync(SelectedExercise);
            ProgressionValues = points.Select(p => Math.Round(p.BestE1RM)).ToArray();
            ProgressionLabels = points.Select(p => p.Date.ToString("d/M")).ToArray();
            HasProgression = points.Count > 0;
        }
        if (invalidate) ChartsUpdated?.Invoke();
    }

    private static string WeekdayLetter(DateTime d)
    {
        var s = d.ToString("ddd", new CultureInfo("sv-SE"));
        return s.Length > 0 ? char.ToUpper(s[0]) + s.Substring(1, Math.Min(2, s.Length - 1)) : s;
    }
}
