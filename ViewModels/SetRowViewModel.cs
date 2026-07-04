using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessQuest.Models;

namespace FitnessQuest.ViewModels;

/// <summary>Editable row for a single set inside the gym editor.</summary>
public partial class SetRowViewModel : ObservableObject
{
    public SetRowViewModel(int number) => _setNumber = number;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Badge))]
    private int _setNumber;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Volume))]
    private string _weight = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Volume))]
    private string _reps = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Badge))]
    [NotifyPropertyChangedFor(nameof(IsWarmup))]
    private SetType _setType = SetType.Normal;

    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private string _previousText = "–";

    /// <summary>Recompute-totals callback set by the parent block.</summary>
    public Action? Changed { get; set; }
    /// <summary>Fired when a set is checked off (parent starts the rest timer).</summary>
    public Action<SetRowViewModel>? CompletedToggled { get; set; }
    /// <summary>Remove-this-row callback set by the parent block.</summary>
    public Action<SetRowViewModel>? RemoveRequested { get; set; }

    [RelayCommand]
    private void Remove() => RemoveRequested?.Invoke(this);

    public double WeightKg => double.TryParse(Weight, out var w) ? w : 0;
    public int RepsValue => int.TryParse(Reps, out var r) ? r : 0;
    public double Volume => WeightKg * RepsValue;
    public bool IsWarmup => SetType == SetType.Warmup;

    /// <summary>Number, or a letter badge for special set types.</summary>
    public string Badge => SetType switch
    {
        SetType.Warmup => "W",
        SetType.Dropset => "D",
        SetType.Failure => "F",
        _ => SetNumber.ToString()
    };

    partial void OnIsCompletedChanged(bool value)
    {
        if (value) CompletedToggled?.Invoke(this);
        Changed?.Invoke();
    }
    partial void OnWeightChanged(string value) => Changed?.Invoke();
    partial void OnRepsChanged(string value) => Changed?.Invoke();

    [RelayCommand]
    private void CycleType()
    {
        SetType = SetType switch
        {
            SetType.Normal => SetType.Warmup,
            SetType.Warmup => SetType.Dropset,
            SetType.Dropset => SetType.Failure,
            _ => SetType.Normal
        };
        Changed?.Invoke();
    }
}
