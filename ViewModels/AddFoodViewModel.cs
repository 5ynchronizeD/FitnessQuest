using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitnessQuest.Data;
using FitnessQuest.Models;
using FitnessQuest.Services;

namespace FitnessQuest.ViewModels;

public partial class AddFoodViewModel : BaseViewModel
{
    private readonly AppDatabase _db;
    private readonly GamificationService _gamification;
    private readonly FeedbackService _feedback;

    private FoodItem _source = new();
    private int _editingEntryId;

    public AddFoodViewModel(AppDatabase db, GamificationService gamification, FeedbackService feedback)
    {
        _db = db;
        _gamification = gamification;
        _feedback = feedback;
    }

    [ObservableProperty] private string _saveLabel = "Logga måltid  (+10 XP)";

    public List<MealType> Meals { get; } =
        new() { MealType.Breakfast, MealType.Lunch, MealType.Dinner, MealType.Snack };

    [ObservableProperty] private string _foodName = string.Empty;
    [ObservableProperty] private string? _brand;
    [ObservableProperty] private string? _imageUrl;
    [ObservableProperty] private string _per100Label = string.Empty;
    [ObservableProperty] private bool _isFavorite;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewKcal))]
    [NotifyPropertyChangedFor(nameof(PreviewProtein))]
    [NotifyPropertyChangedFor(nameof(PreviewCarbs))]
    [NotifyPropertyChangedFor(nameof(PreviewFat))]
    private double _grams = 100;

    [ObservableProperty] private MealType _selectedMeal = MealType.Snack;

    public double PreviewKcal => _source.KcalPer100g * Grams / 100.0;
    public double PreviewProtein => _source.ProteinPer100g * Grams / 100.0;
    public double PreviewCarbs => _source.CarbsPer100g * Grams / 100.0;
    public double PreviewFat => _source.FatPer100g * Grams / 100.0;

    public void Load(FoodItem item)
    {
        _editingEntryId = 0;
        _source = item;
        FoodName = item.Name;
        Brand = item.Brand;
        ImageUrl = item.ImageUrl;
        IsFavorite = item.IsFavorite;
        Per100Label = item.MacroSummary;
        Grams = 100;
        SelectedMeal = GuessMeal();
        SaveLabel = "Logga måltid  (+10 XP)";
        RaisePreview();
    }

    /// <summary>Open an already-logged entry for editing (no XP awarded on save).</summary>
    public void LoadForEdit(FoodLogEntry entry)
    {
        _editingEntryId = entry.Id;
        _source = new FoodItem
        {
            Id = entry.FoodItemId,
            Name = entry.Name,
            KcalPer100g = entry.KcalPer100g,
            ProteinPer100g = entry.ProteinPer100g,
            CarbsPer100g = entry.CarbsPer100g,
            FatPer100g = entry.FatPer100g
        };
        FoodName = entry.Name;
        Brand = null;
        ImageUrl = null;
        IsFavorite = false;
        Per100Label = _source.MacroSummary;
        Grams = entry.Grams;
        SelectedMeal = entry.Meal;
        SaveLabel = "Spara ändringar";
        RaisePreview();
    }

    private void RaisePreview()
    {
        OnPropertyChanged(nameof(PreviewKcal));
        OnPropertyChanged(nameof(PreviewProtein));
        OnPropertyChanged(nameof(PreviewCarbs));
        OnPropertyChanged(nameof(PreviewFat));
    }

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
    private void AdjustGrams(string delta)
    {
        if (double.TryParse(delta, out var d))
            Grams = Math.Max(1, Grams + d);
    }

    [RelayCommand]
    private void ToggleFavorite() => IsFavorite = !IsFavorite;

    [RelayCommand]
    private void SelectMeal(MealType meal) => SelectedMeal = meal;

    [RelayCommand]
    private async Task Save()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;

            if (_editingEntryId != 0)
            {
                // Update an existing entry — no XP, no streak change.
                var existing = await _db.GetFoodLogEntryAsync(_editingEntryId);
                if (existing is not null)
                {
                    existing.Grams = Grams;
                    existing.Meal = SelectedMeal;
                    await _db.UpdateFoodLogAsync(existing);
                }
                WeakReferenceMessenger.Default.Send(new DataChangedMessage("nutrition"));
                await CloseAsync();
                return;
            }

            // Persist / refresh the catalogue entry so it shows up in "recent".
            _source.IsFavorite = IsFavorite;
            var foodId = await _db.UpsertFoodItemAsync(_source);

            var entry = new FoodLogEntry
            {
                FoodItemId = foodId,
                Name = _source.DisplayName,
                Grams = Grams,
                Meal = SelectedMeal,
                KcalPer100g = _source.KcalPer100g,
                ProteinPer100g = _source.ProteinPer100g,
                CarbsPer100g = _source.CarbsPer100g,
                FatPer100g = _source.FatPer100g,
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
}
