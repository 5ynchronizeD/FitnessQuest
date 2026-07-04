using FitnessQuest.Views;

namespace FitnessQuest;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        // Routes reached by explicit navigation (not tabs).
        Routing.RegisterRoute(nameof(BarcodeScanPage), typeof(BarcodeScanPage));
        Routing.RegisterRoute(nameof(WorkoutEditorPage), typeof(WorkoutEditorPage));
        Routing.RegisterRoute(nameof(ExercisePickerPage), typeof(ExercisePickerPage));
        Routing.RegisterRoute(nameof(PlateCalculatorPage), typeof(PlateCalculatorPage));
        Routing.RegisterRoute(nameof(AchievementsPage), typeof(AchievementsPage));
        Routing.RegisterRoute(nameof(CardioDetailPage), typeof(CardioDetailPage));
        Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));
    }
}
