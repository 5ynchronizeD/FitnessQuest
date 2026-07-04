using FitnessQuest.ViewModels;

namespace FitnessQuest.Views;

public partial class WorkoutEditorPage : ContentPage, IQueryAttributable
{
    private readonly WorkoutEditorViewModel _vm;
    private int _workoutId;
    private int _templateId;
    private bool _initialized;

    public WorkoutEditorPage(WorkoutEditorViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("workoutId", out var wv) && int.TryParse(wv?.ToString(), out var id))
            _workoutId = id;
        if (query.TryGetValue("templateId", out var tv) && int.TryParse(tv?.ToString(), out var tid))
            _templateId = tid;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_initialized) return;
        _initialized = true;
        await _vm.InitializeAsync(_workoutId, _templateId);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.StopTimers();
    }
}
