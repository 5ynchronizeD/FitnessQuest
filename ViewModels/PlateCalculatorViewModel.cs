using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessQuest.Services;

namespace FitnessQuest.ViewModels;

public enum PlateMode { Barbell, Dumbbell }

/// <summary>
/// Barbell/dumbbell plate calculator. Keeps a separate plate inventory per mode
/// (with how many of each plate the user owns), respects those counts, and shows
/// exactly which plates to load. All choices persist.
/// </summary>
public partial class PlateCalculatorViewModel : BaseViewModel, IQueryAttributable
{
    private const string PrefMode = "plate_mode";

    private static readonly (double w, int c)[] BarbellDefaults =
        { (25, 2), (20, 2), (15, 2), (10, 2), (5, 2), (2.5, 2), (1.25, 2) };
    private static readonly (double w, int c)[] DumbbellDefaults =
        { (5, 4), (2.5, 4), (2, 6), (1.25, 4), (1, 4), (0.5, 4) };

    private readonly List<PlateOption> _barbell = new();
    private readonly List<PlateOption> _dumbbell = new();
    private bool _loading;

    public PlateCalculatorViewModel()
    {
        Title = "Viktkalkylator";
        LoadInventories();
        var storedMode = Preferences.Get(PrefMode, nameof(PlateMode.Barbell));
        Mode = storedMode == nameof(PlateMode.Dumbbell) ? PlateMode.Dumbbell : PlateMode.Barbell;
        ApplyMode();
    }

    [ObservableProperty] private PlateMode _mode = PlateMode.Barbell;
    [ObservableProperty] private bool _isBarbell = true;
    [ObservableProperty] private string _target = "60";
    [ObservableProperty] private string _bar = "20";
    [ObservableProperty] private string _barLabel = "Stång (kg)";
    [ObservableProperty] private string _perSideCaption = "per sida";
    [ObservableProperty] private string _newPlate = string.Empty;
    [ObservableProperty] private string _perSideText = string.Empty;
    [ObservableProperty] private string _achievedText = string.Empty;
    [ObservableProperty] private bool _isExact = true;
    [ObservableProperty] private string _hint = string.Empty;
    [ObservableProperty] private string _pairNote = string.Empty;

    /// <summary>The active mode's plate inventory.</summary>
    public ObservableCollection<PlateOption> AvailablePlates { get; } = new();

    /// <summary>Flattened list of individual plates (per side) for the visual stack.</summary>
    public ObservableCollection<PlateChip> PlatesPerSide { get; } = new();

    partial void OnTargetChanged(string value) => Recalculate();
    partial void OnBarChanged(string value) { Recalculate(); PersistBar(); }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("target", out var value))
        {
            var s = value?.ToString();
            if (!string.IsNullOrWhiteSpace(s) && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var t) && t > 0)
                Target = t.ToString("0.##");
        }
    }

    [RelayCommand]
    private void SetMode(string mode)
    {
        var m = mode == nameof(PlateMode.Dumbbell) ? PlateMode.Dumbbell : PlateMode.Barbell;
        if (m == Mode) return;
        Mode = m;
        Preferences.Set(PrefMode, Mode.ToString());
        ApplyMode();
    }

    private void ApplyMode()
    {
        IsBarbell = Mode == PlateMode.Barbell;
        BarLabel = IsBarbell ? "Stång (kg)" : "Handtag (kg)";
        PerSideCaption = IsBarbell ? "per sida" : "per sida av hanteln";
        PairNote = IsBarbell ? string.Empty : "Gäller en hantel. För ett par: ladda båda likadant.";

        _loading = true;
        Bar = Preferences.Get(BarPrefKey(), IsBarbell ? "20" : "0");
        _loading = false;

        AvailablePlates.Clear();
        foreach (var p in ActiveList())
            AvailablePlates.Add(p);

        Recalculate();
    }

    [RelayCommand]
    private void Bump(string delta)
    {
        if (double.TryParse(Target, out var t) && double.TryParse(delta, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            Target = Math.Max(0, t + d).ToString("0.##");
    }

    [RelayCommand]
    private void IncreaseCount(PlateOption? plate)
    {
        if (plate is null) return;
        plate.Count++;
        Recalculate();
        PersistInventory();
    }

    [RelayCommand]
    private void DecreaseCount(PlateOption? plate)
    {
        if (plate is null) return;
        plate.Count = Math.Max(0, plate.Count - 1);
        Recalculate();
        PersistInventory();
    }

    [RelayCommand]
    private void AddPlate()
    {
        if (!double.TryParse(NewPlate?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var w) || w <= 0)
            return;
        if (AvailablePlates.Any(p => Math.Abs(p.Weight - w) < 0.001))
        {
            NewPlate = string.Empty;
            return;
        }
        InsertPlate(AvailablePlates, w, 2, isCustom: true);
        ActiveList().Clear();
        ActiveList().AddRange(AvailablePlates);
        NewPlate = string.Empty;
        Recalculate();
        PersistInventory();
    }

    [RelayCommand]
    private void RemovePlate(PlateOption? plate)
    {
        if (plate is null) return;
        AvailablePlates.Remove(plate);
        ActiveList().Remove(plate);
        Recalculate();
        PersistInventory();
    }

    private List<PlateOption> ActiveList() => IsBarbell ? _barbell : _dumbbell;

    private static void InsertPlate(IList<PlateOption> list, double weight, int count, bool isCustom)
    {
        var option = new PlateOption(weight, count) { IsCustom = isCustom };
        int i = 0;
        while (i < list.Count && list[i].Weight > weight) i++;
        list.Insert(i, option);
    }

    private void Recalculate()
    {
        double.TryParse(Target, NumberStyles.Any, CultureInfo.InvariantCulture, out var target);
        double bar = double.TryParse(Bar, NumberStyles.Any, CultureInfo.InvariantCulture, out var b) ? b : 0;

        var specs = AvailablePlates.Select(p => new PlateSpec(p.Weight, p.Count));
        var sol = PlateCalculator.Solve(target, bar, specs);

        PerSideText = sol.PerSideText;
        AchievedText = $"{sol.AchievedWeight:0.##} kg";
        IsExact = sol.IsExact;
        Hint = sol.IsExact
            ? "Exakt träff ✓"
            : sol.LimitedByInventory
                ? $"Slut på skivor – {sol.Remainder:0.##} kg kvar till målet"
                : sol.Remainder > 0
                    ? $"Närmast möjliga – {sol.Remainder:0.##} kg kvar"
                    : "Under stångens vikt";

        PlatesPerSide.Clear();
        foreach (var p in sol.PerSide)
            for (int i = 0; i < p.CountPerSide; i++)
                PlatesPerSide.Add(new PlateChip(p.Weight));
    }

    // ---- Persistence ----
    private string InvPrefKey() => IsBarbell ? "plate_inv_barbell" : "plate_inv_dumbbell";
    private string BarPrefKey() => IsBarbell ? "plate_bar_barbell" : "plate_bar_dumbbell";

    private void LoadInventories()
    {
        LoadOne(_barbell, "plate_inv_barbell", BarbellDefaults);
        LoadOne(_dumbbell, "plate_inv_dumbbell", DumbbellDefaults);
    }

    private static void LoadOne(List<PlateOption> target, string key, (double w, int c)[] defaults)
    {
        var stored = Preferences.Get(key, string.Empty);
        target.Clear();
        if (string.IsNullOrWhiteSpace(stored))
        {
            foreach (var (w, c) in defaults)
                InsertPlate(target, w, c, isCustom: !defaults.Any(d => d.w == w));
            return;
        }
        var standard = defaults.Select(d => d.w).ToHashSet();
        foreach (var token in stored.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = token.Split('=');
            if (parts.Length == 2
                && double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var w)
                && int.TryParse(parts[1], out var c))
                InsertPlate(target, w, c, isCustom: !standard.Contains(w));
        }
        if (target.Count == 0)
            foreach (var (w, c) in defaults)
                InsertPlate(target, w, c, isCustom: false);
    }

    private void PersistInventory()
    {
        if (_loading) return;
        var csv = string.Join(';', AvailablePlates.Select(p =>
            $"{p.Weight.ToString(CultureInfo.InvariantCulture)}={p.Count}"));
        Preferences.Set(InvPrefKey(), csv);
    }

    private void PersistBar()
    {
        if (_loading) return;
        Preferences.Set(BarPrefKey(), Bar ?? "0");
    }
}

/// <summary>A plate the user owns; <see cref="Count"/> is how many in total.</summary>
public partial class PlateOption : ObservableObject
{
    public PlateOption(double weight, int count)
    {
        Weight = weight;
        _count = count;
    }

    public double Weight { get; }
    public bool IsCustom { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAny))]
    private int _count;

    public bool HasAny => Count > 0;
    public string Label => Weight % 1 == 0 ? $"{Weight:0}" : $"{Weight:0.##}";

    public Color Color => Weight switch
    {
        >= 25 => Color.FromArgb("#E63946"),
        >= 20 => Color.FromArgb("#457B9D"),
        >= 15 => Color.FromArgb("#F4A261"),
        >= 10 => Color.FromArgb("#2A9D8F"),
        >= 5 => Color.FromArgb("#264653"),
        >= 2.5 => Color.FromArgb("#8D99AE"),
        _ => Color.FromArgb("#6C757D")
    };
}

/// <summary>A single plate for the visual stack, sized/coloured by weight.</summary>
public class PlateChip
{
    public PlateChip(double weight) => Weight = weight;
    public double Weight { get; }
    public string Label => Weight % 1 == 0 ? $"{Weight:0}" : $"{Weight:0.##}";

    public double Height => Weight switch
    {
        >= 25 => 130,
        >= 20 => 118,
        >= 15 => 104,
        >= 10 => 88,
        >= 5 => 70,
        >= 2.5 => 54,
        _ => 42
    };

    public Color Color => Weight switch
    {
        >= 25 => Color.FromArgb("#E63946"),
        >= 20 => Color.FromArgb("#457B9D"),
        >= 15 => Color.FromArgb("#F4A261"),
        >= 10 => Color.FromArgb("#2A9D8F"),
        >= 5 => Color.FromArgb("#264653"),
        >= 2.5 => Color.FromArgb("#8D99AE"),
        _ => Color.FromArgb("#6C757D")
    };
}
