using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitnessQuest.Data;
using FitnessQuest.Models;
using FitnessQuest.Services;

namespace FitnessQuest.ViewModels;

/// <summary>Searchable exercise catalog; selecting one adds it to the editor.</summary>
public partial class ExercisePickerViewModel : BaseViewModel
{
    private readonly AppDatabase _db;
    private List<Exercise> _all = new();

    public ExercisePickerViewModel(AppDatabase db)
    {
        _db = db;
        Title = "Välj övning";
    }

    public ObservableCollection<Exercise> Exercises { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddCustom))]
    private string _search = string.Empty;

    [ObservableProperty] private bool _hasResults;

    public bool CanAddCustom => !string.IsNullOrWhiteSpace(Search);

    [RelayCommand]
    private async Task Load()
    {
        _all = await _db.GetExercisesAsync();
        ApplyFilter();
    }

    partial void OnSearchChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var q = Search?.Trim();
        IEnumerable<Exercise> filtered = _all;
        if (!string.IsNullOrWhiteSpace(q))
            filtered = _all.Where(e =>
                e.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.MuscleGroup.Contains(q, StringComparison.OrdinalIgnoreCase));

        Exercises.Clear();
        foreach (var e in filtered)
            Exercises.Add(e);
        HasResults = Exercises.Count > 0;
    }

    [RelayCommand]
    private async Task Select(Exercise? exercise)
    {
        if (exercise is null) return;
        WeakReferenceMessenger.Default.Send(new ExercisePickedMessage(exercise));
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task AddCustom()
    {
        if (string.IsNullOrWhiteSpace(Search)) return;
        var ex = await _db.EnsureExerciseAsync(Search.Trim(), Equipment.Barbell);
        WeakReferenceMessenger.Default.Send(new ExercisePickedMessage(ex));
        await Shell.Current.GoToAsync("..");
    }
}
