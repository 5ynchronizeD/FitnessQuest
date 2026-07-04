using FitnessQuest.ViewModels;

namespace FitnessQuest.Views;

public partial class CardioPage : ContentPage
{
    private readonly CardioViewModel _vm;

    public CardioPage(CardioViewModel vm)
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
