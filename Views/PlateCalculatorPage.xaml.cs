using FitnessQuest.ViewModels;

namespace FitnessQuest.Views;

public partial class PlateCalculatorPage : ContentPage
{
    public PlateCalculatorPage(PlateCalculatorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
