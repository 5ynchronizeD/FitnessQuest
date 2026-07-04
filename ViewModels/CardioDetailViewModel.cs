using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessQuest.Data;
using FitnessQuest.Models;

namespace FitnessQuest.ViewModels;

/// <summary>
/// Shows an imported cardio session: summary plus heart-rate over time,
/// elevation profile, and heart-rate grouped by pace.
/// </summary>
public partial class CardioDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly AppDatabase _db;
    private int _id;

    public CardioDetailViewModel(AppDatabase db)
    {
        _db = db;
        Title = "Passdetaljer";
    }

    public event Action? ChartsUpdated;

    [ObservableProperty] private string _header = string.Empty;
    [ObservableProperty] private string _dateText = string.Empty;
    [ObservableProperty] private string _distanceText = "–";
    [ObservableProperty] private string _durationText = "–";
    [ObservableProperty] private string _paceText = "–";
    [ObservableProperty] private string _avgHrText = "–";
    [ObservableProperty] private string _maxHrText = "–";
    [ObservableProperty] private string _elevationText = "–";
    [ObservableProperty] private bool _hasHr;
    [ObservableProperty] private bool _hasElevation;
    [ObservableProperty] private bool _hasPaceHr;
    [ObservableProperty] private bool _hasNoData;

    public double[] HrValues { get; private set; } = Array.Empty<double>();
    public string[] HrLabels { get; private set; } = Array.Empty<string>();
    public double[] EleValues { get; private set; } = Array.Empty<double>();
    public string[] EleLabels { get; private set; } = Array.Empty<string>();
    public double[] PaceHrValues { get; private set; } = Array.Empty<double>();
    public string[] PaceHrLabels { get; private set; } = Array.Empty<string>();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var value) && int.TryParse(value?.ToString(), out var id))
            _id = id;
    }

    [RelayCommand]
    public async Task Load()
    {
        var session = await _db.GetCardioAsync(_id);
        if (session is null) return;

        Header = $"{session.TypeEmoji} {session.TypeName}";
        DateText = session.PerformedAt.ToString("dddd d MMM yyyy, HH:mm");
        DistanceText = $"{session.DistanceKm:0.00} km";
        DurationText = $"{session.DurationMinutes:0} min";
        PaceText = session.PaceDisplay;
        AvgHrText = session.AvgHeartRate > 0 ? $"{session.AvgHeartRate} bpm" : "–";
        MaxHrText = session.MaxHeartRate > 0 ? $"{session.MaxHeartRate} bpm" : "–";
        ElevationText = $"{session.ElevationGainM:0} m";

        var points = string.IsNullOrEmpty(session.TrackJson)
            ? new List<TrackPoint>()
            : JsonSerializer.Deserialize<List<TrackPoint>>(session.TrackJson) ?? new();

        BuildHrOverTime(points);
        BuildElevationOverDistance(points);
        BuildHrPerPace(points);
        HasNoData = !HasHr && !HasElevation && !HasPaceHr;

        ChartsUpdated?.Invoke();
    }

    private void BuildHrOverTime(List<TrackPoint> points)
    {
        var hrPoints = points.Where(p => p.Hr > 0).ToList();
        HasHr = hrPoints.Count >= 2;
        if (!HasHr) return;

        var sampled = Downsample(hrPoints, 40);
        HrValues = sampled.Select(p => (double)p.Hr).ToArray();
        HrLabels = AxisLabels(sampled.Select(p => p.T / 60.0).ToArray(), v => $"{v:0}m");
    }

    private void BuildElevationOverDistance(List<TrackPoint> points)
    {
        var elePoints = points.Where(p => p.Ele != 0).ToList();
        HasElevation = elePoints.Count >= 2 && elePoints.Max(p => p.Ele) - elePoints.Min(p => p.Ele) > 1;
        if (!HasElevation) return;

        var sampled = Downsample(elePoints, 40);
        // Normalise so the lowest point sits near the baseline.
        double min = sampled.Min(p => p.Ele);
        EleValues = sampled.Select(p => p.Ele - min).ToArray();
        EleLabels = AxisLabels(sampled.Select(p => p.DistanceKm).ToArray(), v => $"{v:0.#}km");
    }

    private void BuildHrPerPace(List<TrackPoint> points)
    {
        // Average HR grouped by pace (min/km), bucketed to 0.5-min bins.
        var buckets = new Dictionary<double, (double hrSum, double weight)>();
        for (int i = 1; i < points.Count; i++)
        {
            var a = points[i - 1];
            var b = points[i];
            double dd = b.DistanceKm - a.DistanceKm;
            double dt = (b.T - a.T) / 60.0; // minutes
            if (dd <= 0.001 || dt <= 0 || b.Hr <= 0) continue;

            double pace = dt / dd; // min/km
            if (pace < 2 || pace > 15) continue; // ignore stops/noise

            double bin = Math.Round(pace * 2) / 2.0; // nearest 0.5
            var cur = buckets.TryGetValue(bin, out var v) ? v : (0, 0);
            buckets[bin] = (cur.hrSum + b.Hr * dt, cur.weight + dt);
        }

        var ordered = buckets.Where(kv => kv.Value.weight > 0)
            .OrderBy(kv => kv.Key)
            .ToList();
        HasPaceHr = ordered.Count >= 2;
        if (!HasPaceHr) return;

        PaceHrValues = ordered.Select(kv => Math.Round(kv.Value.hrSum / kv.Value.weight)).ToArray();
        PaceHrLabels = ordered.Select(kv => PaceLabel(kv.Key)).ToArray();
    }

    private static string PaceLabel(double pace)
    {
        int m = (int)pace;
        int s = (int)Math.Round((pace - m) * 60);
        if (s == 60) { m++; s = 0; }
        return $"{m}:{s:00}";
    }

    private static List<TrackPoint> Downsample(List<TrackPoint> points, int target)
    {
        if (points.Count <= target) return points;
        var result = new List<TrackPoint>(target);
        double step = (points.Count - 1) / (double)(target - 1);
        for (int i = 0; i < target; i++)
            result.Add(points[(int)Math.Round(i * step)]);
        return result;
    }

    private static string[] AxisLabels(double[] values, Func<double, string> fmt)
    {
        var labels = new string[values.Length];
        if (values.Length == 0) return labels;
        int marks = 5;
        for (int i = 0; i < values.Length; i++)
        {
            bool show = i == 0 || i == values.Length - 1 || i % Math.Max(1, values.Length / marks) == 0;
            labels[i] = show ? fmt(values[i]) : string.Empty;
        }
        return labels;
    }
}
