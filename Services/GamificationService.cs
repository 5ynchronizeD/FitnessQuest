using FitnessQuest.Data;
using FitnessQuest.Models;

namespace FitnessQuest.Services;

/// <summary>
/// The heart of the "always want to keep logging" loop: awards XP, keeps the
/// daily streak alive, and unlocks achievements. Every logging action funnels
/// through <see cref="RegisterActivityAsync"/>.
/// </summary>
public class GamificationService
{
    private readonly AppDatabase _db;

    public GamificationService(AppDatabase db) => _db = db;

    /// <summary>Base XP per activity kind (excludes performance bonuses).</summary>
    public static int BaseXp(ActivityType type) => type switch
    {
        ActivityType.FoodLog => 10,
        ActivityType.GymWorkout => 50,
        ActivityType.CardioSession => 30,
        _ => 5
    };

    /// <summary>
    /// Applies XP + streak, then re-evaluates achievements. Returns everything
    /// the UI needs to celebrate. <paramref name="bonusXp"/> lets callers add
    /// performance-based XP (e.g. distance or volume).
    /// </summary>
    public async Task<GamificationResult> RegisterActivityAsync(ActivityType type, int bonusXp = 0)
    {
        var state = await _db.GetStateAsync();
        var result = new GamificationResult { OldLevel = state.Level };

        int xp = BaseXp(type) + Math.Max(0, bonusXp);

        // ---- Streak ----
        var today = DateTime.Today;
        var last = state.LastActivityDate?.Date;
        if (last is null)
        {
            state.CurrentStreak = 1;
            result.StreakIncreased = true;
        }
        else if (last == today)
        {
            // Already logged today — streak unchanged.
        }
        else if (last == today.AddDays(-1))
        {
            state.CurrentStreak += 1;
            result.StreakIncreased = true;
        }
        else
        {
            state.CurrentStreak = 1;
            result.StreakIncreased = true;
        }

        // Streak milestone bonus (keeps momentum rewarding).
        if (result.StreakIncreased && state.CurrentStreak > 0 && state.CurrentStreak % 7 == 0)
            xp += 50;

        state.LastActivityDate = today;
        state.LongestStreak = Math.Max(state.LongestStreak, state.CurrentStreak);
        state.TotalXp += xp;

        await _db.SaveStateAsync(state);

        result.XpGained = xp;
        result.StreakAfter = state.CurrentStreak;

        // ---- Achievements (may award further XP) ----
        var unlocked = await EvaluateAchievementsAsync(state);
        result.NewAchievements = unlocked;

        // Re-read level after any achievement XP was added.
        var finalState = await _db.GetStateAsync();
        result.NewLevel = finalState.Level;
        result.LeveledUp = result.NewLevel > result.OldLevel;

        return result;
    }

    /// <summary>
    /// Checks every locked achievement against current aggregate stats and
    /// unlocks any that qualify, granting their XP reward.
    /// </summary>
    public async Task<List<Achievement>> EvaluateAchievementsAsync(GamificationState? state = null)
    {
        state ??= await _db.GetStateAsync();
        var achievements = await _db.GetAchievementsAsync();
        var locked = achievements.Where(a => !a.IsUnlocked).ToList();
        if (locked.Count == 0)
            return new();

        // Gather stats once.
        int foodCount = await _db.CountFoodLogsAsync();
        int gymCount = await _db.CountWorkoutsAsync();
        int cardioCount = await _db.CountCardioAsync();
        double cardioDist = await _db.TotalCardioDistanceAsync();
        double maxWorkoutVolume = (await _db.GetRecentWorkoutsAsync())
            .Select(w => w.TotalVolumeKg).DefaultIfEmpty(0).Max();
        int level = state.Level;
        int streak = state.CurrentStreak;

        var newlyUnlocked = new List<Achievement>();
        int bonusXp = 0;

        foreach (var a in locked)
        {
            bool qualifies = a.Code switch
            {
                "streak_3" => streak >= 3,
                "streak_7" => streak >= 7,
                "streak_30" => streak >= 30,
                "food_first" => foodCount >= 1,
                "food_25" => foodCount >= 25,
                "food_100" => foodCount >= 100,
                "gym_first" => gymCount >= 1,
                "gym_10" => gymCount >= 10,
                "gym_vol" => maxWorkoutVolume >= 1000,
                "cardio_first" => cardioCount >= 1,
                "cardio_10km" => cardioDist >= 10,
                "cardio_100km" => cardioDist >= 100,
                "level_5" => level >= 5,
                "level_10" => level >= 10,
                "level_25" => level >= 25,
                _ => false
            };

            if (qualifies)
            {
                a.IsUnlocked = true;
                a.UnlockedAt = DateTime.Now;
                await _db.SaveAchievementAsync(a);
                newlyUnlocked.Add(a);
                bonusXp += a.XpReward;
            }
        }

        if (bonusXp > 0)
        {
            state.TotalXp += bonusXp;
            await _db.SaveStateAsync(state);
        }

        return newlyUnlocked;
    }
}
