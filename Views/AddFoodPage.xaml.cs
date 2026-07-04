using FitnessQuest.ViewModels;

namespace FitnessQuest.Views;

public partial class AddFoodPage : ContentPage
{
    public AddFoodPage(AddFoodViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
