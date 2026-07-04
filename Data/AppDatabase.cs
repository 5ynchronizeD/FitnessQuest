using FitnessQuest.Models;
using SQLite;

namespace FitnessQuest.Data;

/// <summary>
/// Local SQLite store. All access goes through this single service
/// (registered as a singleton). Connection is opened lazily on first use.
/// </summary>
public class AppDatabase
{
    private SQLiteAsyncConnection? _db;

    private async Task<SQLiteAsyncConnection> Connection()
    {
        if (_db is not null)
            return _db;

        var path = Path.Combine(FileSystem.AppDataDirectory, "fitnessquest.db3");
        _db = new SQLiteAsyncConnection(
            path,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        await _db.CreateTableAsync<UserProfile>();
        await _db.CreateTableAsync<GamificationState>();
        await _db.CreateTableAsync<Achievement>();
        await _db.CreateTableAsync<FoodItem>();
        await _db.CreateTableAsync<FoodLogEntry>();
        await _db.CreateTableAsync<Workout>();
        await _db.CreateTableAsync<WorkoutExercise>();
        await _db.CreateTableAsync<ExerciseSet>();
        await _db.CreateTableAsync<Exercise>();
        await _db.CreateTableAsync<CardioSession>();
        await _db.CreateTableAsync<WeightEntry>();
        await _db.CreateTableAsync<WorkoutTemplate>();

        await EnsureSeedDataAsync(_db);
        return _db;
    }

    private static async Task EnsureSeedDataAsync(SQLiteAsyncConnection db)
    {
        if (await db.Table<UserProfile>().CountAsync() == 0)
            await db.InsertAsync(new UserProfile());

        if (await db.Table<GamificationState>().CountAsync() == 0)
            await db.InsertAsync(new GamificationState());

        if (await db.Table<Achievement>().CountAsync() == 0)
            await db.InsertAllAsync(AchievementCatalog.Seed());

        if (await db.Table<Exercise>().CountAsync() == 0)
            await db.InsertAllAsync(ExerciseCatalog.Seed());
    }

    // ---- Profile ---------------------------------------------------------
    public async Task<UserProfile> GetProfileAsync()
    {
        var db = await Connection();
        return await db.FindAsync<UserProfile>(1) ?? new UserProfile();
    }

    public async Task SaveProfileAsync(UserProfile profile)
    {
        var db = await Connection();
        await db.InsertOrReplaceAsync(profile);
    }

    // ---- Gamification state ---------------------------------------------
    public async Task<GamificationState> GetStateAsync()
    {
        var db = await Connection();
        return await db.FindAsync<GamificationState>(1) ?? new GamificationState();
    }

    public async Task SaveStateAsync(GamificationState state)
    {
        var db = await Connection();
        await db.InsertOrReplaceAsync(state);
    }

    // ---- Achievements ----------------------------------------------------
    public async Task<List<Achievement>> GetAchievementsAsync()
    {
        var db = await Connection();
        return await db.Table<Achievement>().OrderBy(a => a.SortOrder).ToListAsync();
    }

    public async Task SaveAchievementAsync(Achievement a)
    {
        var db = await Connection();
        await db.InsertOrReplaceAsync(a);
    }

    // ---- Food catalogue (recent/favourites) -----------------------------
    public async Task<FoodItem?> FindFoodByBarcodeAsync(string barcode)
    {
        var db = await Connection();
        return await db.Table<FoodItem>().Where(f => f.Barcode == barcode).FirstOrDefaultAsync();
    }

    public async Task<int> UpsertFoodItemAsync(FoodItem item)
    {
        var db = await Connection();
        item.LastUsed = DateTime.Now;
        if (item.Id == 0)
            await db.InsertAsync(item);
        else
            await db.UpdateAsync(item);
        return item.Id;
    }

    public async Task<List<FoodItem>> GetRecentFoodsAsync(int take = 30)
    {
        var db = await Connection();
        return await db.Table<FoodItem>()
            .OrderByDescending(f => f.IsFavorite)
            .ThenByDescending(f => f.LastUsed)
            .Take(take)
            .ToListAsync();
    }

    // ---- Food log --------------------------------------------------------
    public async Task AddFoodLogAsync(FoodLogEntry entry)
    {
        var db = await Connection();
        await db.InsertAsync(entry);
    }

    public async Task DeleteFoodLogAsync(FoodLogEntry entry)
    {
        var db = await Connection();
        await db.DeleteAsync(entry);
    }

    public async Task UpdateFoodLogAsync(FoodLogEntry entry)
    {
        var db = await Connection();
        await db.UpdateAsync(entry);
    }

    public async Task<FoodLogEntry?> GetFoodLogEntryAsync(int id)
    {
        var db = await Connection();
        return await db.FindAsync<FoodLogEntry>(id);
    }

    public async Task<List<FoodLogEntry>> GetFoodLogRangeAsync(DateTime from, DateTime to)
    {
        var db = await Connection();
        return await db.Table<FoodLogEntry>()
            .Where(e => e.LoggedAt >= from && e.LoggedAt < to)
            .ToListAsync();
    }

    public async Task<List<FoodLogEntry>> GetFoodLogForDayAsync(DateTime day)
    {
        var db = await Connection();
        var start = day.Date;
        var end = start.AddDays(1);
        return await db.Table<FoodLogEntry>()
            .Where(e => e.LoggedAt >= start && e.LoggedAt < end)
            .OrderBy(e => e.LoggedAt)
            .ToListAsync();
    }

    public async Task<int> CountFoodLogsAsync()
    {
        var db = await Connection();
        return await db.Table<FoodLogEntry>().CountAsync();
    }

    // ---- Workouts (gym) --------------------------------------------------

    /// <summary>
    /// Persists a full workout graph. For edits (workout.Id != 0) the existing
    /// children are cleared and rewritten, so the in-memory graph is the source
    /// of truth. Rollups are recomputed. Returns the workout id.
    /// </summary>
    public async Task<int> SaveFullWorkoutAsync(
        Workout workout,
        IEnumerable<(WorkoutExercise Exercise, List<ExerciseSet> Sets)> exercises)
    {
        var db = await Connection();
        var graph = exercises.ToList();

        // Recompute rollups (completed sets only count toward volume/sets).
        var allSets = graph.SelectMany(g => g.Sets).ToList();
        workout.TotalSets = allSets.Count;
        workout.TotalVolumeKg = allSets.Where(s => s.SetType != SetType.Warmup).Sum(s => s.Volume);

        if (workout.Id == 0)
            await db.InsertAsync(workout);
        else
        {
            await db.UpdateAsync(workout);
            await ClearWorkoutChildrenAsync(db, workout.Id);
        }

        int order = 0;
        foreach (var (ex, sets) in graph)
        {
            ex.Id = 0;
            ex.WorkoutId = workout.Id;
            ex.OrderIndex = order++;
            await db.InsertAsync(ex);

            int setNo = 0;
            foreach (var s in sets)
            {
                s.Id = 0;
                s.WorkoutExerciseId = ex.Id;
                s.WorkoutId = workout.Id;
                s.SetNumber = ++setNo;
                await db.InsertAsync(s);
            }
        }
        return workout.Id;
    }

    private static async Task ClearWorkoutChildrenAsync(SQLiteAsyncConnection db, int workoutId)
    {
        await db.ExecuteAsync("DELETE FROM ExerciseSet WHERE WorkoutId = ?", workoutId);
        await db.ExecuteAsync("DELETE FROM WorkoutExercise WHERE WorkoutId = ?", workoutId);
    }

    public async Task DeleteWorkoutAsync(int workoutId)
    {
        var db = await Connection();
        await ClearWorkoutChildrenAsync(db, workoutId);
        await db.ExecuteAsync("DELETE FROM Workout WHERE Id = ?", workoutId);
    }

    public async Task<Workout?> GetWorkoutAsync(int id)
    {
        var db = await Connection();
        return await db.FindAsync<Workout>(id);
    }

    public async Task<List<WorkoutExercise>> GetWorkoutExercisesAsync(int workoutId)
    {
        var db = await Connection();
        return await db.Table<WorkoutExercise>()
            .Where(e => e.WorkoutId == workoutId)
            .OrderBy(e => e.OrderIndex)
            .ToListAsync();
    }

    public async Task<List<ExerciseSet>> GetSetsForExerciseAsync(int workoutExerciseId)
    {
        var db = await Connection();
        return await db.Table<ExerciseSet>()
            .Where(s => s.WorkoutExerciseId == workoutExerciseId)
            .OrderBy(s => s.SetNumber)
            .ToListAsync();
    }

    public async Task<List<Workout>> GetRecentWorkoutsAsync(int take = 50)
    {
        var db = await Connection();
        return await db.Table<Workout>()
            .OrderByDescending(w => w.PerformedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountWorkoutsAsync()
    {
        var db = await Connection();
        return await db.Table<Workout>().CountAsync();
    }

    // ---- Exercise catalog ------------------------------------------------
    public async Task<List<Exercise>> GetExercisesAsync()
    {
        var db = await Connection();
        return await db.Table<Exercise>()
            .OrderByDescending(e => e.UsageCount)
            .ThenBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<Exercise> EnsureExerciseAsync(string name, Equipment equipment, string muscleGroup = "Övrigt")
    {
        var db = await Connection();
        var existing = await db.Table<Exercise>()
            .Where(e => e.Name == name).FirstOrDefaultAsync();
        if (existing is not null)
            return existing;

        var ex = new Exercise { Name = name, Equipment = equipment, MuscleGroup = muscleGroup, IsCustom = true };
        await db.InsertAsync(ex);
        return ex;
    }

    public async Task BumpExerciseUsageAsync(string name)
    {
        var db = await Connection();
        await db.ExecuteAsync("UPDATE Exercise SET UsageCount = UsageCount + 1 WHERE Name = ?", name);
    }

    /// <summary>Most recent completed set for an exercise name, for the "förra gången" hint.</summary>
    public async Task<ExerciseSet?> GetLastSetForExerciseNameAsync(string exerciseName)
    {
        var db = await Connection();
        var sql =
            @"SELECT s.* FROM ExerciseSet s
              JOIN WorkoutExercise we ON we.Id = s.WorkoutExerciseId
              WHERE we.ExerciseName = ?
              ORDER BY s.Id DESC LIMIT 1";
        var rows = await db.QueryAsync<ExerciseSet>(sql, exerciseName);
        return rows.FirstOrDefault();
    }

    // ---- Cardio ----------------------------------------------------------
    public async Task AddCardioAsync(CardioSession session)
    {
        var db = await Connection();
        await db.InsertAsync(session);
    }

    public async Task UpdateCardioAsync(CardioSession session)
    {
        var db = await Connection();
        await db.UpdateAsync(session);
    }

    public async Task DeleteCardioAsync(CardioSession session)
    {
        var db = await Connection();
        await db.DeleteAsync(session);
    }

    public async Task<CardioSession?> GetCardioAsync(int id)
    {
        var db = await Connection();
        return await db.FindAsync<CardioSession>(id);
    }

    public async Task<List<CardioSession>> GetRecentCardioAsync(int take = 50)
    {
        var db = await Connection();
        return await db.Table<CardioSession>()
            .OrderByDescending(c => c.PerformedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<CardioSession>> GetCardioRangeAsync(DateTime from, DateTime to)
    {
        var db = await Connection();
        return await db.Table<CardioSession>()
            .Where(c => c.PerformedAt >= from && c.PerformedAt < to)
            .ToListAsync();
    }

    public async Task<int> CountCardioAsync()
    {
        var db = await Connection();
        return await db.Table<CardioSession>().CountAsync();
    }

    public async Task<double> TotalCardioDistanceAsync()
    {
        var db = await Connection();
        var all = await db.Table<CardioSession>().ToListAsync();
        return all.Sum(c => c.DistanceKm);
    }

    public async Task<List<Workout>> GetWorkoutsRangeAsync(DateTime from, DateTime to)
    {
        var db = await Connection();
        return await db.Table<Workout>()
            .Where(w => w.PerformedAt >= from && w.PerformedAt < to)
            .ToListAsync();
    }

    /// <summary>
    /// Progression data for one exercise: the best estimated 1RM per day it was
    /// trained, oldest first. Used by the progression chart.
    /// </summary>
    public async Task<List<(DateTime Date, double BestE1RM, double TopWeight)>> GetExerciseProgressionAsync(string exerciseName)
    {
        var db = await Connection();
        var sql =
            @"SELECT s.WeightKg, s.Reps, w.PerformedAt
              FROM ExerciseSet s
              JOIN WorkoutExercise we ON we.Id = s.WorkoutExerciseId
              JOIN Workout w ON w.Id = we.WorkoutId
              WHERE we.ExerciseName = ? AND s.WeightKg > 0 AND s.Reps > 0";
        var rows = await db.QueryAsync<ProgressionRow>(sql, exerciseName);
        return rows
            .GroupBy(r => r.PerformedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => (
                g.Key,
                g.Max(r => r.WeightKg * (1 + r.Reps / 30.0)),
                g.Max(r => r.WeightKg)))
            .ToList();
    }

    private class ProgressionRow
    {
        public double WeightKg { get; set; }
        public int Reps { get; set; }
        public DateTime PerformedAt { get; set; }
    }

    /// <summary>Best estimated 1RM ever recorded for an exercise (0 if none).</summary>
    public async Task<double> GetBestE1RMForExerciseAsync(string exerciseName)
    {
        var db = await Connection();
        var sql =
            @"SELECT s.WeightKg, s.Reps FROM ExerciseSet s
              JOIN WorkoutExercise we ON we.Id = s.WorkoutExerciseId
              WHERE we.ExerciseName = ? AND s.WeightKg > 0 AND s.Reps > 0";
        var rows = await db.QueryAsync<ProgressionRow>(sql, exerciseName);
        return rows.Select(r => r.WeightKg * (1 + r.Reps / 30.0)).DefaultIfEmpty(0).Max();
    }

    // ---- Body weight ----------------------------------------------------
    public async Task AddWeightAsync(WeightEntry entry)
    {
        var db = await Connection();
        await db.InsertAsync(entry);
    }

    public async Task DeleteWeightAsync(WeightEntry entry)
    {
        var db = await Connection();
        await db.DeleteAsync(entry);
    }

    public async Task<List<WeightEntry>> GetWeightsAsync(int take = 90)
    {
        var db = await Connection();
        return await db.Table<WeightEntry>()
            .OrderByDescending(w => w.LoggedAt)
            .Take(take)
            .ToListAsync();
    }

    // ---- Workout templates ----------------------------------------------
    public async Task SaveTemplateAsync(WorkoutTemplate template)
    {
        var db = await Connection();
        if (template.Id == 0)
            await db.InsertAsync(template);
        else
            await db.UpdateAsync(template);
    }

    public async Task<List<WorkoutTemplate>> GetTemplatesAsync()
    {
        var db = await Connection();
        return await db.Table<WorkoutTemplate>()
            .OrderByDescending(t => t.LastUsed)
            .ToListAsync();
    }

    public async Task<WorkoutTemplate?> GetTemplateAsync(int id)
    {
        var db = await Connection();
        return await db.FindAsync<WorkoutTemplate>(id);
    }

    public async Task DeleteTemplateAsync(int id)
    {
        var db = await Connection();
        await db.ExecuteAsync("DELETE FROM WorkoutTemplate WHERE Id = ?", id);
    }

    public async Task BumpTemplateUsageAsync(int id)
    {
        var db = await Connection();
        var t = await GetTemplateAsync(id);
        if (t is not null)
        {
            t.LastUsed = DateTime.Now;
            await db.UpdateAsync(t);
        }
    }
}
