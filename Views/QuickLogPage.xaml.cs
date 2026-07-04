using FitnessQuest.ViewModels;

namespace FitnessQuest.Views;

public partial class QuickLogPage : ContentPage
{
    public QuickLogPage(QuickLogViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
