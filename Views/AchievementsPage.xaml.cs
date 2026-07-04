using FitnessQuest.ViewModels;

namespace FitnessQuest.Views;

public partial class AchievementsPage : ContentPage
{
    private readonly AchievementsViewModel _vm;

    public AchievementsPage(AchievementsViewModel vm)
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
