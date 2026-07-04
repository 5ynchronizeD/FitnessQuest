using FitnessQuest.ViewModels;

namespace FitnessQuest.Views;

public partial class ExercisePickerPage : ContentPage
{
    private readonly ExercisePickerViewModel _vm;

    public ExercisePickerPage(ExercisePickerViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadCommand.Execute(null);
    }
}
