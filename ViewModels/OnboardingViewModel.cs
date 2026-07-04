using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitnessQuest.Data;
using FitnessQuest.Models;
using FitnessQuest.Services;

namespace FitnessQuest.ViewModels;

/// <summary>First-run setup: name + body metrics → suggested goals.</summary>
public partial class OnboardingViewModel : BaseViewModel
{
    public const string OnboardedKey = "onboarded";

    private readonly AppDatabase _db;

    public OnboardingViewModel(AppDatabase db)
    {
        _db = db;
        Title = "Välkommen";
    }

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TdeePreview))] private string _weight = "75";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TdeePreview))] private string _height = "178";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TdeePreview))] private string _age = "30";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TdeePreview))] [NotifyPropertyChangedFor(nameof(IsMale))] private Sex _sex = Sex.Male;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TdeePreview))] private ActivityLevel _activity = ActivityLevel.Moderate;

    public bool IsMale => Sex == Sex.Male;
    public List<ActivityLevel> ActivityLevels { get; } = new()
    {
        ActivityLevel.Sedentary, ActivityLevel.Light, ActivityLevel.Moderate,
        ActivityLevel.Active, ActivityLevel.VeryActive
    };

    public string TdeePreview
    {
        get
        {
            var t = NutritionTargets.Suggest(Build());
            return $"Förslag: {t.Calories} kcal · {t.Protein}P / {t.Carbs}K / {t.Fat}F";
        }
    }

    [RelayCommand] private void SelectSex(string sex) => Sex = sex == nameof(Sex.Female) ? Sex.Female : Sex.Male;
    [RelayCommand] private void SelectActivity(ActivityLevel level) => Activity = level;

    [RelayCommand]
    private async Task Finish()
    {
        var p = Build();
        var t = NutritionTargets.Suggest(p);
        p.CalorieGoal = t.Calories;
        p.ProteinGoal = t.Protein;
        p.CarbsGoal = t.Carbs;
        p.FatGoal = t.Fat;
        await _db.SaveProfileAsync(p);

        // Seed the first weight entry so the diary has a starting point.
        await _db.AddWeightAsync(new WeightEntry { WeightKg = p.WeightKg, LoggedAt = DateTime.Now });

        Preferences.Set(OnboardedKey, true);
        WeakReferenceMessenger.Default.Send(new DataChangedMessage("nutrition"));
        await CloseAsync();
    }

    [RelayCommand]
    private async Task Skip()
    {
        Preferences.Set(OnboardedKey, true);
        await CloseAsync();
    }

    private UserProfile Build() => new()
    {
        Id = 1,
        Name = string.IsNullOrWhiteSpace(Name) ? "Athlete" : Name.Trim(),
        WeightKg = ParseD(Weight, 75),
        HeightCm = ParseD(Height, 178),
        Age = ParseI(Age, 30),
        Sex = Sex,
        ActivityLevel = Activity
    };

    private static double ParseD(string s, double f) =>
        double.TryParse(s?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) && v > 0 ? v : f;
    private static int ParseI(string s, int f) => int.TryParse(s, out var v) && v > 0 ? v : f;

    private static async Task CloseAsync()
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is { } page && page.Navigation.ModalStack.Count > 0)
            await page.Navigation.PopModalAsync();
    }
}
