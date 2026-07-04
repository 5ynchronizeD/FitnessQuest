using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitnessQuest.Data;
using FitnessQuest.Models;
using FitnessQuest.Services;

namespace FitnessQuest.ViewModels;

/// <summary>
/// Fast meal logging: just type kcal + macros, pick a meal, done. No food
/// search, no database lookup — for when you already know the numbers.
/// </summary>
public partial class QuickLogViewModel : BaseViewModel
{
    private readonly AppDatabase _db;
    private readonly GamificationService _gamification;
    private readonly FeedbackService _feedback;

    public QuickLogViewModel(AppDatabase db, GamificationService gamification, FeedbackService feedback)
    {
        _db = db;
        _gamification = gamification;
        _feedback = feedback;
        Title = "Snabblogg";
        SelectedMeal = GuessMeal();
    }

    [ObservableProperty] private string _foodName = "Snabblogg";
    [ObservableProperty] private string _kcal = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KcalFromMacros))]
    [NotifyPropertyChangedFor(nameof(ShowMacroKcal))]
    private string _protein = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KcalFromMacros))]
    [NotifyPropertyChangedFor(nameof(ShowMacroKcal))]
    private string _carbs = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KcalFromMacros))]
    [NotifyPropertyChangedFor(nameof(ShowMacroKcal))]
    private string _fat = string.Empty;

    [ObservableProperty] private MealType _selectedMeal;

    /// <summary>Estimated kcal from macros (4/4/9), shown as a helper.</summary>
    public double KcalFromMacros => P() * 4 + C() * 4 + F() * 9;
    public bool ShowMacroKcal => KcalFromMacros > 0;

    private double P() => Parse(Protein);
    private double C() => Parse(Carbs);
    private double F() => Parse(Fat);
    private static double Parse(string s) =>
        double.TryParse(s?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static MealType GuessMeal()
    {
        var h = DateTime.Now.Hour;
        return h switch
        {
            >= 5 and < 10 => MealType.Breakfast,
            >= 10 and < 14 => MealType.Lunch,
            >= 14 and < 17 => MealType.Snack,
            >= 17 and < 22 => MealType.Dinner,
            _ => MealType.Snack
        };
    }

    [RelayCommand]
    private void SelectMeal(MealType meal) => SelectedMeal = meal;

    [RelayCommand]
    private void UseMacroKcal() => Kcal = Math.Round(KcalFromMacros).ToString("0");

    [RelayCommand]
    private async Task Save()
    {
        if (IsBusy) return;

        double kcal = Parse(Kcal);
        if (kcal <= 0) kcal = KcalFromMacros; // fall back to computed kcal
        if (kcal <= 0 && P() <= 0 && C() <= 0 && F() <= 0)
        {
            await AlertAsync("Fyll i något", "Ange minst kalorier eller makros.");
            return;
        }

        try
        {
            IsBusy = true;
            // Store absolute values by using grams = 100 and per-100g = the entered totals.
            var entry = new FoodLogEntry
            {
                FoodItemId = 0,
                Name = string.IsNullOrWhiteSpace(FoodName) ? "Snabblogg" : FoodName.Trim(),
                Grams = 100,
                Meal = SelectedMeal,
                KcalPer100g = kcal,
                ProteinPer100g = P(),
                CarbsPer100g = C(),
                FatPer100g = F(),
                LoggedAt = DateTime.Now
            };
            await _db.AddFoodLogAsync(entry);

            var result = await _gamification.RegisterActivityAsync(ActivityType.FoodLog);
            WeakReferenceMessenger.Default.Send(new DataChangedMessage("nutrition"));

            await CloseAsync();
            await _feedback.CelebrateAsync(result);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task Cancel() => CloseAsync();

    private static async Task CloseAsync()
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is { } page)
            await page.Navigation.PopModalAsync();
    }

    private static async Task AlertAsync(string title, string message)
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is { } page)
            await page.DisplayAlert(title, message, "OK");
    }
}
