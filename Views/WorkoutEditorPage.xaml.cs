using FitnessQuest.ViewModels;

namespace FitnessQuest.Views;

public partial class WorkoutEditorPage : ContentPage, IQueryAttributable
{
    private readonly WorkoutEditorViewModel _vm;
    private int _workoutId;
    private bool _initialized;

    public WorkoutEditorPage(WorkoutEditorViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("workoutId", out var value) && int.TryParse(value?.ToString(), out var id))
            _workoutId = id;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_initialized) return;
        _initialized = true;
        await _vm.InitializeAsync(_workoutId);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.StopTimers();
    }
}
