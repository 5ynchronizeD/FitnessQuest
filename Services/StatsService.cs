using FitnessQuest.Data;

namespace FitnessQuest.Services;

public record DailyNutrition(DateTime Date, double Kcal, double Protein, double Carbs, double Fat);
public record WeeklyTraining(DateTime WeekStart, double VolumeKg, int Workouts);
public record DailyValue(DateTime Date, double Value);
public record ProgressionPoint(DateTime Date, double BestE1RM, double TopWeight);

/// <summary>Aggregations that back the statistics/charts screen.</summary>
public class StatsService
{
    private readonly AppDatabase _db;
    public StatsService(AppDatabase db) => _db = db;

    /// <summary>Per-day nutrition totals for the last <paramref name="days"/> days (zero-filled, oldest first).</summary>
    public async Task<List<DailyNutrition>> GetNutritionDailyAsync(int days = 7)
    {
        var today = DateTime.Today;
        var from = today.AddDays(-(days - 1));
        var entries = await _db.GetFoodLogRangeAsync(from, today.AddDays(1));
        var byDay = entries.GroupBy(e => e.LoggedAt.Date)
            .ToDictionary(g => g.Key, g => g);

        var result = new List<DailyNutrition>();
        for (int i = 0; i < days; i++)
        {
            var d = from.AddDays(i);
            if (byDay.TryGetValue(d, out var g))
                result.Add(new DailyNutrition(d, g.Sum(e => e.Kcal), g.Sum(e => e.Protein),
                    g.Sum(e => e.Carbs), g.Sum(e => e.Fat)));
            else
                result.Add(new DailyNutrition(d, 0, 0, 0, 0));
        }
        return result;
    }

    /// <summary>Training volume and workout count per week for the last <paramref name="weeks"/> weeks.</summary>
    public async Task<List<WeeklyTraining>> GetTrainingWeeklyAsync(int weeks = 8)
    {
        var thisWeekStart = StartOfWeek(DateTime.Today);
        var from = thisWeekStart.AddDays(-7 * (weeks - 1));
        var workouts = await _db.GetWorkoutsRangeAsync(from, DateTime.Today.AddDays(1));
        var byWeek = workouts.GroupBy(w => StartOfWeek(w.PerformedAt.Date))
            .ToDictionary(g => g.Key, g => g);

        var result = new List<WeeklyTraining>();
        for (int i = 0; i < weeks; i++)
        {
            var ws = from.AddDays(7 * i);
            if (byWeek.TryGetValue(ws, out var g))
                result.Add(new WeeklyTraining(ws, g.Sum(w => w.TotalVolumeKg), g.Count()));
            else
                result.Add(new WeeklyTraining(ws, 0, 0));
        }
        return result;
    }

    /// <summary>Cardio distance per day for the last <paramref name="days"/> days (zero-filled).</summary>
    public async Task<List<DailyValue>> GetCardioDailyAsync(int days = 14)
    {
        var today = DateTime.Today;
        var from = today.AddDays(-(days - 1));
        var sessions = await _db.GetCardioRangeAsync(from, today.AddDays(1));
        var byDay = sessions.GroupBy(s => s.PerformedAt.Date)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.DistanceKm));

        var result = new List<DailyValue>();
        for (int i = 0; i < days; i++)
        {
            var d = from.AddDays(i);
            result.Add(new DailyValue(d, byDay.TryGetValue(d, out var v) ? v : 0));
        }
        return result;
    }

    public async Task<List<ProgressionPoint>> GetExerciseProgressionAsync(string exerciseName)
    {
        var raw = await _db.GetExerciseProgressionAsync(exerciseName);
        return raw.Select(r => new ProgressionPoint(r.Date, r.BestE1RM, r.TopWeight)).ToList();
    }

    /// <summary>Names of exercises the user has actually trained (for the progression picker).</summary>
    public async Task<List<string>> GetTrainedExerciseNamesAsync()
    {
        var workouts = await _db.GetRecentWorkoutsAsync(500);
        var names = new List<string>();
        foreach (var w in workouts)
        {
            var exs = await _db.GetWorkoutExercisesAsync(w.Id);
            names.AddRange(exs.Select(e => e.ExerciseName));
        }
        return names.Where(n => !string.IsNullOrWhiteSpace(n))
            .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .ToList();
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        // Monday-based week.
        int diff = (7 + (int)date.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        return date.Date.AddDays(-diff);
    }
}
