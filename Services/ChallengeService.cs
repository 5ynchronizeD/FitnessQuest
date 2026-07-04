using FitnessQuest.Data;
using FitnessQuest.Models;

namespace FitnessQuest.Services;

public record ChallengeResult(List<DailyChallenge> Challenges, GamificationResult? Reward);

/// <summary>
/// Generates and evaluates daily challenges. Three are chosen per day
/// (rotating deterministically) and XP is awarded once each is completed.
/// </summary>
public class ChallengeService
{
    private readonly AppDatabase _db;
    private readonly GamificationService _gamification;

    public ChallengeService(AppDatabase db, GamificationService gamification)
    {
        _db = db;
        _gamification = gamification;
    }

    private record Pool(string Code, string Desc, int Xp);

    private static readonly Pool[] Catalog =
    {
        new("food3", "Logga 3 måltider", 20),
        new("protein", "Nå ditt proteinmål", 30),
        new("train", "Träna idag (gym eller cardio)", 30),
        new("water", "Drick ditt vattenmål", 15),
        new("weigh", "Logga din vikt", 10),
        new("cardio", "Kör ett cardiopass", 25),
    };

    /// <summary>Ensures today's challenges exist, evaluates progress, awards XP for newly completed.</summary>
    public async Task<ChallengeResult> EnsureAndEvaluateAsync()
    {
        var today = DateTime.Today;
        var profile = await _db.GetProfileAsync();
        var challenges = await _db.GetChallengesForDayAsync(today);

        if (challenges.Count == 0)
        {
            int start = today.DayOfYear % Catalog.Length;
            var chosen = new List<DailyChallenge>();
            for (int i = 0; i < 3; i++)
            {
                var pool = Catalog[(start + i) % Catalog.Length];
                chosen.Add(new DailyChallenge
                {
                    DateKey = today.ToString("yyyy-MM-dd"),
                    Code = pool.Code,
                    Description = pool.Desc,
                    Target = TargetFor(pool.Code, profile),
                    XpReward = pool.Xp
                });
            }
            await _db.InsertChallengesAsync(chosen);
            challenges = await _db.GetChallengesForDayAsync(today);
        }

        // Compute today's stats once.
        var food = await _db.GetFoodLogForDayAsync(today);
        int foodCount = food.Count;
        double protein = food.Sum(f => f.Protein);
        var workouts = (await _db.GetRecentWorkoutsAsync()).Count(w => w.PerformedAt.Date == today);
        var cardio = (await _db.GetRecentCardioAsync()).Count(c => c.PerformedAt.Date == today);
        int water = await _db.GetWaterAsync(today);
        bool weighedToday = (await _db.GetWeightsAsync(10)).Any(w => w.LoggedAt.Date == today);

        int awardXp = 0;
        foreach (var c in challenges)
        {
            c.Progress = c.Code switch
            {
                "food3" => foodCount,
                "protein" => (int)protein,
                "train" => workouts + cardio,
                "water" => water,
                "weigh" => weighedToday ? 1 : 0,
                "cardio" => cardio,
                _ => 0
            };

            if (c.Completed && !c.Claimed)
            {
                c.Claimed = true;
                awardXp += c.XpReward;
                await _db.UpdateChallengeAsync(c);
            }
        }

        GamificationResult? reward = null;
        if (awardXp > 0)
            reward = await _gamification.AwardXpAsync(awardXp);

        return new ChallengeResult(challenges.OrderBy(c => c.Completed).ToList(), reward);
    }

    private static int TargetFor(string code, UserProfile p) => code switch
    {
        "food3" => 3,
        "protein" => p.ProteinGoal,
        "train" => 1,
        "water" => p.WaterGoalGlasses,
        "weigh" => 1,
        "cardio" => 1,
        _ => 1
    };
}
