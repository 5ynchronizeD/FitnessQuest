using FitnessQuest.ViewModels;

namespace FitnessQuest.Views;

public partial class NutritionPage : ContentPage
{
    private readonly NutritionViewModel _vm;

    public NutritionPage(NutritionViewModel vm)
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
