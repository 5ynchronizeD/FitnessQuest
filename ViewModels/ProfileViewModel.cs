using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitnessQuest.Data;
using FitnessQuest.Models;
using FitnessQuest.Services;

namespace FitnessQuest.ViewModels;

public partial class ProfileViewModel : BaseViewModel
{
    private readonly AppDatabase _db;
    private readonly INotificationService _notifications;
    private bool _loadingReminder;

    public ProfileViewModel(AppDatabase db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
        Title = "Profil";
        _loadingReminder = true;
        _reminderEnabled = Preferences.Get("reminder_enabled", false);
        _reminderHour = Preferences.Get("reminder_hour", 20);
        _loadingReminder = false;
    }

    public event Action? ChartUpdated;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TdeePreview))]
    private string _weight = "75";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TdeePreview))]
    private string _height = "178";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TdeePreview))]
    private string _age = "30";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TdeePreview))]
    [NotifyPropertyChangedFor(nameof(IsMale))]
    private Sex _sex = Sex.Male;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TdeePreview))]
    private ActivityLevel _activity = ActivityLevel.Moderate;

    [ObservableProperty] private string _calorieGoal = "2200";
    [ObservableProperty] private string _proteinGoal = "150";
    [ObservableProperty] private string _carbsGoal = "220";
    [ObservableProperty] private string _fatGoal = "70";

    [ObservableProperty] private string _newWeight = string.Empty;
    [ObservableProperty] private bool _hasWeightHistory;
    [ObservableProperty] private string _weightTrend = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReminderLabel))]
    private bool _reminderEnabled;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReminderLabel))]
    private int _reminderHour = 20;

    public string ReminderLabel => ReminderEnabled ? $"Påminner kl {ReminderHour:00}:00 varje dag" : "Av";

    partial void OnReminderEnabledChanged(bool value) => ApplyReminder();
    partial void OnReminderHourChanged(int value) => ApplyReminder();

    private void ApplyReminder()
    {
        if (_loadingReminder) return;
        ReminderHour = Math.Clamp(ReminderHour, 5, 23);
        Preferences.Set("reminder_enabled", ReminderEnabled);
        Preferences.Set("reminder_hour", ReminderHour);
        _notifications.SetDailyReminder(ReminderEnabled, ReminderHour, 0);
    }

    [RelayCommand] private void ReminderEarlier() => ReminderHour = Math.Max(5, ReminderHour - 1);
    [RelayCommand] private void ReminderLater() => ReminderHour = Math.Min(23, ReminderHour + 1);

    public List<ActivityLevel> ActivityLevels { get; } = new()
    {
        ActivityLevel.Sedentary, ActivityLevel.Light, ActivityLevel.Moderate,
        ActivityLevel.Active, ActivityLevel.VeryActive
    };

    public bool IsMale => Sex == Sex.Male;

    public double[] WeightValues { get; private set; } = Array.Empty<double>();
    public string[] WeightLabels { get; private set; } = Array.Empty<string>();

    public string TdeePreview
    {
        get
        {
            var p = BuildProfile();
            return $"BMR ≈ {NutritionTargets.Bmr(p):0} kcal · TDEE ≈ {NutritionTargets.Tdee(p)} kcal/dag";
        }
    }

    [RelayCommand]
    public async Task Load()
    {
        var p = await _db.GetProfileAsync();
        Name = p.Name;
        Weight = p.WeightKg.ToString("0.#", CultureInfo.InvariantCulture);
        Height = p.HeightCm.ToString("0.#", CultureInfo.InvariantCulture);
        Age = p.Age.ToString();
        Sex = p.Sex;
        Activity = p.ActivityLevel;
        CalorieGoal = p.CalorieGoal.ToString();
        ProteinGoal = p.ProteinGoal.ToString();
        CarbsGoal = p.CarbsGoal.ToString();
        FatGoal = p.FatGoal.ToString();
        await LoadWeightsAsync();
    }

    private async Task LoadWeightsAsync()
    {
        var weights = await _db.GetWeightsAsync(60);
        HasWeightHistory = weights.Count >= 1;
        // Oldest first for the chart.
        var ordered = weights.OrderBy(w => w.LoggedAt).ToList();
        WeightValues = ordered.Select(w => w.WeightKg).ToArray();
        WeightLabels = ordered.Select((w, i) =>
            i == 0 || i == ordered.Count - 1 || i % Math.Max(1, ordered.Count / 4) == 0
                ? w.LoggedAt.ToString("d/M") : string.Empty).ToArray();

        if (ordered.Count >= 2)
        {
            double diff = ordered[^1].WeightKg - ordered[0].WeightKg;
            string arrow = diff > 0.05 ? "↑" : diff < -0.05 ? "↓" : "→";
            WeightTrend = $"{arrow} {Math.Abs(diff):0.0} kg sedan start";
        }
        else WeightTrend = string.Empty;

        ChartUpdated?.Invoke();
    }

    [RelayCommand] private void SelectSex(string sex) => Sex = sex == nameof(Sex.Female) ? Sex.Female : Sex.Male;
    [RelayCommand] private void SelectActivity(ActivityLevel level) => Activity = level;

    [RelayCommand]
    private void SuggestGoals()
    {
        var t = NutritionTargets.Suggest(BuildProfile());
        CalorieGoal = t.Calories.ToString();
        ProteinGoal = t.Protein.ToString();
        CarbsGoal = t.Carbs.ToString();
        FatGoal = t.Fat.ToString();
    }

    [RelayCommand]
    private async Task Save()
    {
        var p = BuildProfile();
        p.CalorieGoal = ParseI(CalorieGoal, 2200);
        p.ProteinGoal = ParseI(ProteinGoal, 150);
        p.CarbsGoal = ParseI(CarbsGoal, 220);
        p.FatGoal = ParseI(FatGoal, 70);
        await _db.SaveProfileAsync(p);
        WeakReferenceMessenger.Default.Send(new DataChangedMessage("nutrition"));
        await AlertAsync("Sparat", "Din profil och dina mål är uppdaterade.");
    }

    [RelayCommand]
    private async Task LogWeight()
    {
        if (!double.TryParse(NewWeight?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var w) || w <= 0)
            return;
        await _db.AddWeightAsync(new WeightEntry { WeightKg = w, LoggedAt = DateTime.Now });

        // Keep the profile's current weight in sync with the latest measurement.
        Weight = w.ToString("0.#", CultureInfo.InvariantCulture);
        var p = await _db.GetProfileAsync();
        p.WeightKg = w;
        await _db.SaveProfileAsync(p);

        NewWeight = string.Empty;
        await LoadWeightsAsync();
    }

    private UserProfile BuildProfile() => new()
    {
        Id = 1,
        Name = string.IsNullOrWhiteSpace(Name) ? "Athlete" : Name.Trim(),
        WeightKg = ParseD(Weight, 75),
        HeightCm = ParseD(Height, 178),
        Age = ParseI(Age, 30),
        Sex = Sex,
        ActivityLevel = Activity,
        CalorieGoal = ParseI(CalorieGoal, 2200),
        ProteinGoal = ParseI(ProteinGoal, 150),
        CarbsGoal = ParseI(CarbsGoal, 220),
        FatGoal = ParseI(FatGoal, 70)
    };

    private static double ParseD(string s, double fallback) =>
        double.TryParse(s?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) && v > 0 ? v : fallback;
    private static int ParseI(string s, int fallback) =>
        int.TryParse(s, out var v) && v > 0 ? v : fallback;

    private static async Task AlertAsync(string title, string message)
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is { } page)
            await page.DisplayAlert(title, message, "OK");
    }
}
