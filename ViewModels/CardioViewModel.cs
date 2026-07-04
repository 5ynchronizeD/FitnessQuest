using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitnessQuest.Data;
using FitnessQuest.Models;
using FitnessQuest.Services;

namespace FitnessQuest.ViewModels;

public partial class CardioViewModel : BaseViewModel
{
    private readonly AppDatabase _db;
    private readonly GamificationService _gamification;
    private readonly FeedbackService _feedback;

    public CardioViewModel(AppDatabase db, GamificationService gamification, FeedbackService feedback)
    {
        _db = db;
        _gamification = gamification;
        _feedback = feedback;
        Title = "Cardio";
    }

    public ObservableCollection<CardioSession> RecentSessions { get; } = new();
    public List<CardioType> Types { get; } =
        new() { CardioType.Running, CardioType.Cycling, CardioType.Walking };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PaceDisplay))]
    private CardioType _selectedType = CardioType.Running;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PaceDisplay))]
    private string _distance = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PaceDisplay))]
    private string _duration = string.Empty;

    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private bool _hasHistory;
    [ObservableProperty] private double _totalDistance;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _saveLabel = "✅ Spara pass";

    private int _editingId;

    public string PaceDisplay
    {
        get
        {
            if (double.TryParse(Distance, out var km) && double.TryParse(Duration, out var min)
                && km > 0 && min > 0)
            {
                var preview = new CardioSession { DistanceKm = km, DurationMinutes = min };
                return SelectedType == CardioType.Cycling
                    ? $"{preview.SpeedKmh:0.0} km/h"
                    : preview.PaceDisplay;
            }
            return "–";
        }
    }

    [RelayCommand]
    private void SelectType(CardioType type) => SelectedType = type;

    [RelayCommand]
    private void EditSession(CardioSession? session)
    {
        if (session is null) return;
        _editingId = session.Id;
        SelectedType = session.Type;
        Distance = session.DistanceKm > 0 ? session.DistanceKm.ToString("0.##") : string.Empty;
        Duration = session.DurationMinutes > 0 ? session.DurationMinutes.ToString("0.##") : string.Empty;
        Notes = session.Notes ?? string.Empty;
        IsEditing = true;
        SaveLabel = "💾 Spara ändringar";
    }

    [RelayCommand]
    private void CancelEdit()
    {
        _editingId = 0;
        IsEditing = false;
        SaveLabel = "✅ Spara pass";
        Distance = string.Empty;
        Duration = string.Empty;
        Notes = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteSession(CardioSession? session)
    {
        if (session is null) return;
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is not null)
        {
            bool ok = await page.DisplayAlert("Ta bort pass", $"Ta bort {session.TypeName.ToLower()} {session.DateDisplay}?", "Ta bort", "Avbryt");
            if (!ok) return;
        }
        await _db.DeleteCardioAsync(session);
        if (_editingId == session.Id) CancelEdit();
        await Load();
    }

    [RelayCommand]
    private async Task Load()
    {
        var sessions = await _db.GetRecentCardioAsync();
        RecentSessions.Clear();
        foreach (var s in sessions)
            RecentSessions.Add(s);
        HasHistory = RecentSessions.Count > 0;
        TotalDistance = await _db.TotalCardioDistanceAsync();
    }

    [RelayCommand]
    private async Task Save()
    {
        if (IsBusy) return;
        double.TryParse(Distance, out var km);
        double.TryParse(Duration, out var min);
        if (km <= 0 && min <= 0)
        {
            await AlertAsync("Fyll i pass", "Ange distans och/eller tid.");
            return;
        }

        try
        {
            IsBusy = true;

            if (_editingId != 0)
            {
                // Update existing session — no XP.
                var existing = await _db.GetCardioAsync(_editingId);
                if (existing is not null)
                {
                    existing.Type = SelectedType;
                    existing.DistanceKm = km;
                    existing.DurationMinutes = min;
                    existing.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
                    await _db.UpdateCardioAsync(existing);
                }
                CancelEdit();
                WeakReferenceMessenger.Default.Send(new DataChangedMessage("cardio"));
                await Load();
                return;
            }

            var session = new CardioSession
            {
                Type = SelectedType,
                DistanceKm = km,
                DurationMinutes = min,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                PerformedAt = DateTime.Now
            };
            await _db.AddCardioAsync(session);

            // Bonus XP: 5 XP per km.
            int bonus = (int)(km * 5);
            var result = await _gamification.RegisterActivityAsync(ActivityType.CardioSession, bonus);

            Distance = string.Empty;
            Duration = string.Empty;
            Notes = string.Empty;
            WeakReferenceMessenger.Default.Send(new DataChangedMessage("cardio"));

            await Load();
            await _feedback.CelebrateAsync(result);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static async Task AlertAsync(string title, string message)
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is { } page)
            await page.DisplayAlert(title, message, "OK");
    }
}
