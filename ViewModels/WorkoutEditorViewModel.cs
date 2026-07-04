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
/// The Strong-style gym editor: build/edit a workout with multiple exercises,
/// each with editable set rows, set types, supersets, per-exercise rest timer,
/// a running session clock and a rest countdown.
/// </summary>
public partial class WorkoutEditorViewModel : BaseViewModel
{
    private readonly AppDatabase _db;
    private readonly GamificationService _gamification;
    private readonly FeedbackService _feedback;

    private IDispatcherTimer? _sessionTimer;
    private IDispatcherTimer? _restTimer;
    private DateTime _startedAt = DateTime.Now;
    private int _editingWorkoutId;
    private int _nextSupersetGroup = 1;

    public WorkoutEditorViewModel(AppDatabase db, GamificationService gamification, FeedbackService feedback)
    {
        _db = db;
        _gamification = gamification;
        _feedback = feedback;
        WeakReferenceMessenger.Default.Register<ExercisePickedMessage>(this,
            (_, m) => AddExerciseBlock(m.Value));
    }

    public ObservableCollection<ExerciseBlockViewModel> Blocks { get; } = new();

    [ObservableProperty] private string _workoutName = "Gympass";
    [ObservableProperty] private string _elapsedLabel = "0:00";
    [ObservableProperty] private bool _hasBlocks;
    [ObservableProperty] private double _totalVolume;
    [ObservableProperty] private int _totalSets;

    // Rest timer
    [ObservableProperty] private bool _restActive;
    [ObservableProperty] private int _restRemaining;
    [ObservableProperty] private string _restLabel = "0:00";

    public bool IsEditing => _editingWorkoutId != 0;

    // ---- Loading --------------------------------------------------------
    public async Task InitializeAsync(int workoutId)
    {
        Blocks.Clear();
        _editingWorkoutId = workoutId;

        if (workoutId == 0)
        {
            WorkoutName = SuggestName();
            _startedAt = DateTime.Now;
            StartSessionTimer();
            return;
        }

        // Editing an existing workout.
        var workout = await _db.GetWorkoutAsync(workoutId);
        if (workout is null) return;
        WorkoutName = workout.Name;
        _startedAt = workout.PerformedAt;

        var exercises = await _db.GetWorkoutExercisesAsync(workoutId);
        foreach (var we in exercises)
        {
            var block = CreateBlock(we.ExerciseName, we.Equipment);
            block.RestSeconds = we.RestSeconds;
            block.SupersetGroup = we.SupersetGroup;
            _nextSupersetGroup = Math.Max(_nextSupersetGroup, we.SupersetGroup + 1);

            var sets = await _db.GetSetsForExerciseAsync(we.Id);
            foreach (var s in sets)
            {
                block.AddSetRow(new SetRowViewModel(s.SetNumber)
                {
                    Weight = s.WeightKg > 0 ? s.WeightKg.ToString("0.##") : string.Empty,
                    Reps = s.Reps > 0 ? s.Reps.ToString() : string.Empty,
                    SetType = s.SetType,
                    IsCompleted = s.IsCompleted
                });
            }
            block.Recalculate();
            Blocks.Add(block);
        }
        RecalculateTotals();
    }

    private static string SuggestName()
    {
        var h = DateTime.Now.Hour;
        var part = h < 11 ? "Morgonpass" : h < 15 ? "Lunchpass" : h < 20 ? "Kvällspass" : "Pass";
        return $"{part} {DateTime.Now:d MMM}";
    }

    // ---- Exercises ------------------------------------------------------
    [RelayCommand]
    private Task AddExercise() => Shell.Current.GoToAsync(nameof(ExercisePickerPage));

    private async void AddExerciseBlock(Exercise exercise)
    {
        await _db.BumpExerciseUsageAsync(exercise.Name);
        var block = CreateBlock(exercise.Name, exercise.Equipment);

        // Prefill first set from the last time this exercise was trained.
        var last = await _db.GetLastSetForExerciseNameAsync(exercise.Name);
        var row = new SetRowViewModel(1);
        if (last is not null)
        {
            row.Weight = last.WeightKg > 0 ? last.WeightKg.ToString("0.##") : string.Empty;
            row.Reps = last.Reps > 0 ? last.Reps.ToString() : string.Empty;
            row.PreviousText = $"{last.WeightKg:0} kg × {last.Reps}";
        }
        block.AddSetRow(row);
        block.Recalculate();
        Blocks.Add(block);
        RecalculateTotals();
    }

    private ExerciseBlockViewModel CreateBlock(string name, Equipment equipment)
    {
        var block = new ExerciseBlockViewModel(name, equipment)
        {
            Changed = RecalculateTotals,
            SetCompleted = OnSetCompleted,
            RemoveRequested = RemoveBlock,
            SupersetToggleRequested = ToggleSuperset,
            PlateCalcRequested = OpenPlateCalcForBlock
        };
        return block;
    }

    private void RemoveBlock(ExerciseBlockViewModel block)
    {
        Blocks.Remove(block);
        RecalculateTotals();
    }

    private void ToggleSuperset(ExerciseBlockViewModel block)
    {
        int index = Blocks.IndexOf(block);
        if (index <= 0)
            return; // needs a preceding exercise to pair with

        if (block.SupersetGroup > 0)
        {
            block.SupersetGroup = 0;
        }
        else
        {
            var prev = Blocks[index - 1];
            int group = prev.SupersetGroup > 0 ? prev.SupersetGroup : _nextSupersetGroup++;
            prev.SupersetGroup = group;
            block.SupersetGroup = group;
        }
    }

    private async void OpenPlateCalcForBlock(ExerciseBlockViewModel block)
    {
        var top = block.Sets.Select(s => s.WeightKg).DefaultIfEmpty(0).Max();
        await Shell.Current.GoToAsync($"{nameof(PlateCalculatorPage)}?target={top:0.##}");
    }

    private void RecalculateTotals()
    {
        TotalVolume = Blocks.Sum(b => b.TotalVolume);
        TotalSets = Blocks.Sum(b => b.Sets.Count);
        HasBlocks = Blocks.Count > 0;
    }

    // ---- Rest timer -----------------------------------------------------
    private void OnSetCompleted(SetRowViewModel set)
    {
        var block = Blocks.FirstOrDefault(b => b.Sets.Contains(set));
        if (block is null || block.RestSeconds <= 0) return;
        StartRest(block.RestSeconds);
    }

    private void StartRest(int seconds)
    {
        RestRemaining = seconds;
        RestActive = true;
        UpdateRestLabel();

        _restTimer?.Stop();
        _restTimer = Application.Current!.Dispatcher.CreateTimer();
        _restTimer.Interval = TimeSpan.FromSeconds(1);
        _restTimer.Tick += (_, _) =>
        {
            RestRemaining--;
            UpdateRestLabel();
            if (RestRemaining <= 0)
            {
                _restTimer?.Stop();
                RestActive = false;
                try { HapticFeedback.Perform(HapticFeedbackType.LongPress); } catch { }
            }
        };
        _restTimer.Start();
    }

    private void UpdateRestLabel() =>
        RestLabel = $"{Math.Max(0, RestRemaining) / 60}:{Math.Max(0, RestRemaining) % 60:00}";

    [RelayCommand]
    private void AddRest() { RestRemaining += 15; UpdateRestLabel(); }

    [RelayCommand]
    private void SkipRest()
    {
        _restTimer?.Stop();
        RestActive = false;
    }

    // ---- Session timer --------------------------------------------------
    private void StartSessionTimer()
    {
        _sessionTimer?.Stop();
        _sessionTimer = Application.Current!.Dispatcher.CreateTimer();
        _sessionTimer.Interval = TimeSpan.FromSeconds(1);
        _sessionTimer.Tick += (_, _) =>
        {
            var e = DateTime.Now - _startedAt;
            ElapsedLabel = e.Hours > 0
                ? $"{e.Hours}:{e.Minutes:00}:{e.Seconds:00}"
                : $"{e.Minutes}:{e.Seconds:00}";
        };
        _sessionTimer.Start();
    }

    public void StopTimers()
    {
        _sessionTimer?.Stop();
        _restTimer?.Stop();
    }

    // ---- Save / cancel --------------------------------------------------
    [RelayCommand]
    private async Task Finish()
    {
        if (IsBusy) return;
        if (Blocks.Count == 0)
        {
            await AlertAsync("Tomt pass", "Lägg till minst en övning innan du sparar.");
            return;
        }

        try
        {
            IsBusy = true;
            StopTimers();

            var workout = new Workout
            {
                Id = _editingWorkoutId,
                Name = string.IsNullOrWhiteSpace(WorkoutName) ? "Gympass" : WorkoutName.Trim(),
                PerformedAt = _startedAt,
                DurationSeconds = IsEditing ? 0 : (int)(DateTime.Now - _startedAt).TotalSeconds
            };

            var graph = Blocks.Select(b =>
            {
                var we = new WorkoutExercise
                {
                    ExerciseName = b.ExerciseName,
                    Equipment = b.Equipment,
                    RestSeconds = b.RestSeconds,
                    SupersetGroup = b.SupersetGroup
                };
                var sets = b.Sets.Select(s => new ExerciseSet
                {
                    Reps = s.RepsValue,
                    WeightKg = s.WeightKg,
                    SetType = s.SetType,
                    IsCompleted = s.IsCompleted
                }).ToList();
                return (we, sets);
            }).ToList();

            await _db.SaveFullWorkoutAsync(workout, graph);

            GamificationResult? result = null;
            if (!IsEditing)
            {
                int bonus = (int)(workout.TotalVolumeKg / 200);
                result = await _gamification.RegisterActivityAsync(ActivityType.GymWorkout, bonus);
            }

            WeakReferenceMessenger.Default.Send(new DataChangedMessage("gym"));
            await Shell.Current.GoToAsync("..");
            if (result is not null)
                await _feedback.CelebrateAsync(result);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Cancel()
    {
        StopTimers();
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private Task OpenPlateCalculator() => Shell.Current.GoToAsync(nameof(PlateCalculatorPage));

    private static async Task AlertAsync(string title, string message)
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is { } page)
            await page.DisplayAlert(title, message, "OK");
    }
}
