using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitnessQuest.Data;
using FitnessQuest.Models;
using FitnessQuest.Services;
using FitnessQuest.Views;

namespace FitnessQuest.ViewModels;

/// <summary>
/// Gym home: start a new workout, browse history (tap to edit), open the plate
/// calculator. The actual logging happens in <see cref="WorkoutEditorViewModel"/>.
/// </summary>
public partial class GymWorkoutViewModel : BaseViewModel
{
    private readonly AppDatabase _db;

    public GymWorkoutViewModel(AppDatabase db)
    {
        _db = db;
        Title = "Gym";
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this,
            (_, m) => { if (m.Value == "gym") _ = LoadAsync(); });
    }

    public ObservableCollection<Workout> RecentWorkouts { get; } = new();
    public ObservableCollection<WorkoutTemplate> Templates { get; } = new();

    [ObservableProperty] private bool _hasHistory;
    [ObservableProperty] private bool _hasTemplates;
    [ObservableProperty] private int _totalWorkouts;
    [ObservableProperty] private double _volumeThisWeek;

    [RelayCommand]
    private async Task Load() => await LoadAsync();

    private async Task LoadAsync()
    {
        var workouts = await _db.GetRecentWorkoutsAsync();
        RecentWorkouts.Clear();
        foreach (var w in workouts)
            RecentWorkouts.Add(w);
        HasHistory = RecentWorkouts.Count > 0;
        TotalWorkouts = RecentWorkouts.Count;

        var weekStart = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));
        VolumeThisWeek = workouts.Where(w => w.PerformedAt >= weekStart).Sum(w => w.TotalVolumeKg);

        var templates = await _db.GetTemplatesAsync();
        Templates.Clear();
        foreach (var t in templates)
            Templates.Add(t);
        HasTemplates = Templates.Count > 0;
    }

    [RelayCommand]
    private Task StartWorkout() => Shell.Current.GoToAsync(nameof(WorkoutEditorPage));

    [RelayCommand]
    private Task EditWorkout(Workout? w)
    {
        if (w is null) return Task.CompletedTask;
        return Shell.Current.GoToAsync($"{nameof(WorkoutEditorPage)}?workoutId={w.Id}");
    }

    [RelayCommand]
    private async Task DeleteWorkout(Workout? w)
    {
        if (w is null) return;
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is not null)
        {
            bool ok = await page.DisplayAlert("Ta bort pass", $"Ta bort \"{w.Name}\"?", "Ta bort", "Avbryt");
            if (!ok) return;
        }
        await _db.DeleteWorkoutAsync(w.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private Task StartFromTemplate(WorkoutTemplate? t)
    {
        if (t is null) return Task.CompletedTask;
        return Shell.Current.GoToAsync($"{nameof(WorkoutEditorPage)}?templateId={t.Id}");
    }

    [RelayCommand]
    private async Task DeleteTemplate(WorkoutTemplate? t)
    {
        if (t is null) return;
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is not null)
        {
            bool ok = await page.DisplayAlert("Ta bort mall", $"Ta bort \"{t.Name}\"?", "Ta bort", "Avbryt");
            if (!ok) return;
        }
        await _db.DeleteTemplateAsync(t.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private Task OpenPlateCalculator() => Shell.Current.GoToAsync(nameof(PlateCalculatorPage));
}
