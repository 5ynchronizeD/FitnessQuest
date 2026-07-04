using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitnessQuest.Data;
using FitnessQuest.Models;
using FitnessQuest.Services;
using FitnessQuest.Views;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessQuest.ViewModels;

public partial class NutritionViewModel : BaseViewModel
{
    private readonly AppDatabase _db;
    private readonly OpenFoodFactsService _off;
    private readonly IServiceProvider _services;
    private readonly GamificationService _gamification;
    private bool _lookingUp;

    public NutritionViewModel(AppDatabase db, OpenFoodFactsService off, IServiceProvider services, GamificationService gamification)
    {
        _db = db;
        _off = off;
        _services = services;
        _gamification = gamification;
        Title = "Kost";

        WeakReferenceMessenger.Default.Register<BarcodeScannedMessage>(this,
            (_, m) => _ = HandleBarcodeAsync(m.Value));
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this,
            (_, m) => { if (m.Value == "nutrition") _ = LoadAsync(); });
    }

    public ObservableCollection<FoodLogEntry> TodayEntries { get; } = new();
    public ObservableCollection<FoodItem> RecentFoods { get; } = new();
    public ObservableCollection<FoodItem> SearchResults { get; } = new();

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private bool _hasSearchResults;

    [ObservableProperty] private double _totalKcal;
    [ObservableProperty] private int _calorieGoal = 2200;
    [ObservableProperty] private double _calorieProgress;
    [ObservableProperty] private string _calorieLabel = "0 / 2200 kcal";
    [ObservableProperty] private double _totalProtein;
    [ObservableProperty] private double _totalCarbs;
    [ObservableProperty] private double _totalFat;
    [ObservableProperty] private bool _hasEntries;

    [ObservableProperty] private string _suggestionText = string.Empty;
    [ObservableProperty] private bool _hasSuggestion;
    private string _dismissedSuggestion = string.Empty;

    [RelayCommand]
    private async Task Load() => await LoadAsync();

    private async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var profile = await _db.GetProfileAsync();
            CalorieGoal = profile.CalorieGoal;

            var entries = await _db.GetFoodLogForDayAsync(DateTime.Today);
            TodayEntries.Clear();
            foreach (var e in entries.OrderByDescending(e => e.LoggedAt))
                TodayEntries.Add(e);
            HasEntries = TodayEntries.Count > 0;

            TotalKcal = entries.Sum(e => e.Kcal);
            TotalProtein = entries.Sum(e => e.Protein);
            TotalCarbs = entries.Sum(e => e.Carbs);
            TotalFat = entries.Sum(e => e.Fat);
            CalorieProgress = Math.Min(1, TotalKcal / Math.Max(1, CalorieGoal));
            CalorieLabel = $"{TotalKcal:0} / {CalorieGoal} kcal";

            var recents = await _db.GetRecentFoodsAsync();
            RecentFoods.Clear();
            foreach (var f in recents)
                RecentFoods.Add(f);

            // Quiet, timely balance tip (null most of the time).
            var tip = NutritionAdvisor.Suggest(TotalKcal, TotalProtein, TotalCarbs, TotalFat,
                profile, DateTime.Now.Hour, entries.Count);
            SuggestionText = tip ?? string.Empty;
            HasSuggestion = tip is not null && tip != _dismissedSuggestion;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void DismissSuggestion()
    {
        _dismissedSuggestion = SuggestionText;
        HasSuggestion = false;
    }

    [RelayCommand]
    private async Task QuickLog()
    {
        var page = _services.GetRequiredService<QuickLogPage>();
        if (Application.Current?.Windows.FirstOrDefault()?.Page is { } current)
            await current.Navigation.PushModalAsync(page);
    }

    [RelayCommand]
    private async Task ScanBarcode()
    {
        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            await AlertAsync("Kamera behövs", "Ge appen kameratillstånd för att skanna streckkoder.");
            return;
        }
        await Shell.Current.GoToAsync(nameof(BarcodeScanPage));
    }

    private async Task HandleBarcodeAsync(string barcode)
    {
        if (_lookingUp) return;
        try
        {
            _lookingUp = true;
            // Give the scan page a moment to pop.
            await Task.Delay(350);

            var local = await _db.FindFoodByBarcodeAsync(barcode);
            var item = local ?? await _off.GetByBarcodeAsync(barcode);

            if (item is null)
            {
                await AlertAsync("Hittades inte",
                    $"Produkten med streckkod {barcode} finns inte i databasen. Sök på namn istället.");
                return;
            }
            await OpenAddFoodAsync(item);
        }
        finally
        {
            _lookingUp = false;
        }
    }

    [RelayCommand]
    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;
        try
        {
            IsSearching = true;
            var results = await _off.SearchAsync(SearchQuery.Trim());
            SearchResults.Clear();
            foreach (var r in results)
                SearchResults.Add(r);
            HasSearchResults = SearchResults.Count > 0;
            if (!HasSearchResults)
                await AlertAsync("Inga träffar", "Prova ett annat sökord.");
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        SearchResults.Clear();
        HasSearchResults = false;
    }

    [RelayCommand]
    private async Task SelectFood(FoodItem? item)
    {
        if (item is null) return;
        await OpenAddFoodAsync(item);
    }

    [RelayCommand]
    private async Task DeleteEntry(FoodLogEntry? entry)
    {
        if (entry is null) return;
        await _db.DeleteFoodLogAsync(entry);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task CopyYesterday()
    {
        var yesterday = await _db.GetFoodLogForDayAsync(DateTime.Today.AddDays(-1));
        if (yesterday.Count == 0)
        {
            await AlertAsync("Inget att kopiera", "Du loggade ingen mat igår.");
            return;
        }
        var now = DateTime.Now;
        foreach (var e in yesterday)
        {
            await _db.AddFoodLogAsync(new FoodLogEntry
            {
                FoodItemId = e.FoodItemId,
                Name = e.Name,
                Grams = e.Grams,
                Meal = e.Meal,
                KcalPer100g = e.KcalPer100g,
                ProteinPer100g = e.ProteinPer100g,
                CarbsPer100g = e.CarbsPer100g,
                FatPer100g = e.FatPer100g,
                LoggedAt = now
            });
        }
        await _gamification.RegisterActivityAsync(ActivityType.FoodLog);
        WeakReferenceMessenger.Default.Send(new DataChangedMessage("nutrition"));
        await LoadAsync();
        await AlertAsync("Kopierat", $"{yesterday.Count} måltider från igår lades till idag.");
    }

    [RelayCommand]
    private async Task EditEntry(FoodLogEntry? entry)
    {
        if (entry is null) return;
        var page = _services.GetRequiredService<AddFoodPage>();
        if (page.BindingContext is AddFoodViewModel vm)
            vm.LoadForEdit(entry);
        if (Application.Current?.Windows.FirstOrDefault()?.Page is { } current)
            await current.Navigation.PushModalAsync(page);
    }

    private async Task OpenAddFoodAsync(FoodItem item)
    {
        var page = _services.GetRequiredService<AddFoodPage>();
        if (page.BindingContext is AddFoodViewModel vm)
            vm.Load(item);

        if (Application.Current?.Windows.FirstOrDefault()?.Page is { } current)
            await current.Navigation.PushModalAsync(page);
    }

    private static async Task AlertAsync(string title, string message)
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is { } page)
            await page.DisplayAlert(title, message, "OK");
    }
}
