using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessQuest.Models;

namespace FitnessQuest.ViewModels;

/// <summary>One exercise (with its sets) inside the gym editor.</summary>
public partial class ExerciseBlockViewModel : ObservableObject
{
    public ExerciseBlockViewModel(string name, Equipment equipment)
    {
        _exerciseName = name;
        _equipment = equipment;
    }

    [ObservableProperty] private string _exerciseName;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBarbell))]
    [NotifyPropertyChangedFor(nameof(EquipmentLabel))]
    private Equipment _equipment;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RestLabel))]
    private int _restSeconds = 120;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SupersetLabel))]
    [NotifyPropertyChangedFor(nameof(IsSuperset))]
    private int _supersetGroup;

    [ObservableProperty] private double _totalVolume;

    public ObservableCollection<SetRowViewModel> Sets { get; } = new();

    // Callbacks wired by the editor VM.
    public Action? Changed { get; set; }
    public Action<SetRowViewModel>? SetCompleted { get; set; }
    public Action<ExerciseBlockViewModel>? RemoveRequested { get; set; }
    public Action<ExerciseBlockViewModel>? SupersetToggleRequested { get; set; }
    public Action<ExerciseBlockViewModel>? PlateCalcRequested { get; set; }
    public Func<string, Task<string?>>? PromptPrevious { get; set; }

    public bool IsBarbell => Equipment == Equipment.Barbell;
    public string EquipmentLabel => Equipment.ToSwedish();
    public bool IsSuperset => SupersetGroup > 0;
    public string SupersetLabel => SupersetGroup > 0 ? $"🔗 Superset {SupersetGroup}" : string.Empty;
    public string RestLabel => $"{RestSeconds / 60}:{RestSeconds % 60:00}";

    public void AddSetRow(SetRowViewModel row)
    {
        WireRow(row);
        Sets.Add(row);
        Renumber();
    }

    private void WireRow(SetRowViewModel row)
    {
        row.Changed = () => { Recalculate(); Changed?.Invoke(); };
        row.CompletedToggled = s => SetCompleted?.Invoke(s);
        row.RemoveRequested = RemoveSetRow;
    }

    private void RemoveSetRow(SetRowViewModel row)
    {
        Sets.Remove(row);
        Renumber();
        Recalculate();
        Changed?.Invoke();
    }

    [RelayCommand]
    private void AddSet()
    {
        var last = Sets.LastOrDefault();
        var row = new SetRowViewModel(Sets.Count + 1)
        {
            Weight = last?.Weight ?? string.Empty,
            Reps = last?.Reps ?? string.Empty,
            PreviousText = last is not null ? $"{last.WeightKg:0} kg × {last.RepsValue}" : "–"
        };
        AddSetRow(row);
        Recalculate();
        Changed?.Invoke();
    }

    [RelayCommand]
    private void RemoveSet(SetRowViewModel? row)
    {
        if (row is null) return;
        Sets.Remove(row);
        Renumber();
        Recalculate();
        Changed?.Invoke();
    }

    [RelayCommand] private void Remove() => RemoveRequested?.Invoke(this);
    [RelayCommand] private void ToggleSuperset() => SupersetToggleRequested?.Invoke(this);
    [RelayCommand] private void PlateCalc() => PlateCalcRequested?.Invoke(this);

    [RelayCommand]
    private void IncreaseRest() => RestSeconds = Math.Min(600, RestSeconds + 15);
    [RelayCommand]
    private void DecreaseRest() => RestSeconds = Math.Max(0, RestSeconds - 15);

    private void Renumber()
    {
        int n = 1;
        foreach (var s in Sets)
        {
            // Warmups don't take a working-set number; still show W badge.
            s.SetNumber = s.SetType == SetType.Warmup ? 0 : n++;
        }
    }

    public void Recalculate()
    {
        Renumber();
        TotalVolume = Sets.Where(s => s.SetType != SetType.Warmup).Sum(s => s.Volume);
    }
}
