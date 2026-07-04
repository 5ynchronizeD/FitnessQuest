using System.Text.Json;
using FitnessQuest.Data;

namespace FitnessQuest.Services;

/// <summary>Exports all local data as a single JSON file the user can share/back up.</summary>
public class ExportService
{
    private readonly AppDatabase _db;
    public ExportService(AppDatabase db) => _db = db;

    public async Task<string> WriteExportFileAsync()
    {
        var profile = await _db.GetProfileAsync();
        var state = await _db.GetStateAsync();
        var achievements = await _db.GetAchievementsAsync();
        var foods = await _db.GetFoodLogRangeAsync(new DateTime(2000, 1, 1), DateTime.Now.AddDays(1));
        var cardio = await _db.GetRecentCardioAsync(100000);
        var weights = await _db.GetWeightsAsync(100000);
        var templates = await _db.GetTemplatesAsync();

        var workouts = await _db.GetRecentWorkoutsAsync(100000);
        var workoutGraph = new List<object>();
        foreach (var w in workouts)
        {
            var exercises = await _db.GetWorkoutExercisesAsync(w.Id);
            var exList = new List<object>();
            foreach (var e in exercises)
            {
                var sets = await _db.GetSetsForExerciseAsync(e.Id);
                exList.Add(new { e.ExerciseName, Equipment = e.Equipment.ToString(), e.RestSeconds, e.SupersetGroup, Sets = sets });
            }
            workoutGraph.Add(new { w.Name, w.PerformedAt, w.DurationSeconds, w.TotalSets, w.TotalVolumeKg, Exercises = exList });
        }

        var export = new
        {
            exportedAt = DateTime.Now,
            app = "FitnessQuest",
            profile,
            state = new { state.TotalXp, state.CurrentStreak, state.LongestStreak, state.LastActivityDate, state.Level },
            achievements = achievements.Where(a => a.IsUnlocked).Select(a => new { a.Code, a.Title, a.UnlockedAt }),
            foodLog = foods,
            workouts = workoutGraph,
            cardio,
            weights,
            templates
        };

        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
        var path = Path.Combine(FileSystem.CacheDirectory, $"fitnessquest-export-{DateTime.Now:yyyyMMdd-HHmm}.json");
        await File.WriteAllTextAsync(path, json);
        return path;
    }
}
