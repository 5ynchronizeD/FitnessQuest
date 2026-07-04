using FitnessQuest.ViewModels;

namespace FitnessQuest.Views;

public partial class GymSessionPage : ContentPage
{
    private readonly GymWorkoutViewModel _vm;

    public GymSessionPage(GymWorkoutViewModel vm)
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
