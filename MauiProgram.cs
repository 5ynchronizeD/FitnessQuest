using CommunityToolkit.Maui;
using FitnessQuest.Data;
using FitnessQuest.Services;
using FitnessQuest.ViewModels;
using FitnessQuest.Views;
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;

namespace FitnessQuest;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ---- Services ----
        builder.Services.AddSingleton<AppDatabase>();
        builder.Services.AddSingleton<GamificationService>();
        builder.Services.AddSingleton<FeedbackService>();
        builder.Services.AddSingleton<StatsService>();
        builder.Services.AddSingleton<WorkoutImportService>();
        builder.Services.AddSingleton<INotificationService, Platforms.Android.NotificationService>();
        builder.Services.AddSingleton(_ => new OpenFoodFactsService(new HttpClient()));

        // ---- ViewModels ----
        builder.Services.AddSingleton<DashboardViewModel>();
        builder.Services.AddSingleton<NutritionViewModel>();
        builder.Services.AddSingleton<GymWorkoutViewModel>();
        builder.Services.AddSingleton<CardioViewModel>();
        builder.Services.AddSingleton<AchievementsViewModel>();
        builder.Services.AddSingleton<StatisticsViewModel>();
        builder.Services.AddTransient<AddFoodViewModel>();
        builder.Services.AddTransient<QuickLogViewModel>();
        builder.Services.AddTransient<WorkoutEditorViewModel>();
        builder.Services.AddTransient<ExercisePickerViewModel>();
        builder.Services.AddTransient<PlateCalculatorViewModel>();
        builder.Services.AddTransient<CardioDetailViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<OnboardingViewModel>();

        // ---- Pages ----
        builder.Services.AddSingleton<DashboardPage>();
        builder.Services.AddSingleton<NutritionPage>();
        builder.Services.AddSingleton<GymSessionPage>();
        builder.Services.AddSingleton<CardioPage>();
        builder.Services.AddTransient<AchievementsPage>();
        builder.Services.AddSingleton<StatisticsPage>();
        builder.Services.AddTransient<AddFoodPage>();
        builder.Services.AddTransient<QuickLogPage>();
        builder.Services.AddTransient<BarcodeScanPage>();
        builder.Services.AddTransient<WorkoutEditorPage>();
        builder.Services.AddTransient<ExercisePickerPage>();
        builder.Services.AddTransient<PlateCalculatorPage>();
        builder.Services.AddTransient<CardioDetailPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<OnboardingPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
