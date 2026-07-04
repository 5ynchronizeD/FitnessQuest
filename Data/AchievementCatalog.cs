using FitnessQuest.Models;

namespace FitnessQuest.Data;

/// <summary>
/// The full set of achievements seeded on first launch. The engine
/// (<see cref="Services.GamificationService"/>) knows how to evaluate each
/// <see cref="Achievement.Code"/> against the user's aggregate stats.
/// </summary>
public static class AchievementCatalog
{
    public static List<Achievement> Seed() => new()
    {
        // Streak
        New("streak_3",  "Igång!",            "Logga 3 dagar i rad",        "🔥", AchievementCategory.Streak, 50,  1),
        New("streak_7",  "En vecka stark",    "7 dagars streak",            "🔥", AchievementCategory.Streak, 120, 2),
        New("streak_30", "Obruten månad",     "30 dagars streak",           "🏆", AchievementCategory.Streak, 500, 3),

        // Nutrition
        New("food_first", "Första tuggan",    "Logga din första måltid",    "🍎", AchievementCategory.Nutrition, 20,  10),
        New("food_25",    "Matdagboksförare", "Logga 25 måltider",          "📒", AchievementCategory.Nutrition, 100, 11),
        New("food_100",   "Näringsnörd",      "Logga 100 måltider",         "🥗", AchievementCategory.Nutrition, 300, 12),

        // Gym
        New("gym_first", "Järnviljan",        "Slutför ditt första gympass", "🏋️", AchievementCategory.Gym, 40,  20),
        New("gym_10",    "Stamgäst",          "10 gympass",                  "💪", AchievementCategory.Gym, 150, 21),
        New("gym_vol",   "Tonvis",            "1 000 kg total volym i ett pass", "🏗️", AchievementCategory.Gym, 200, 22),

        // Cardio
        New("cardio_first", "Igång och rullar", "Logga ditt första cardiopass", "🏃", AchievementCategory.Cardio, 40,  30),
        New("cardio_10km",  "Milslukaren",      "10 km totalt",                 "🚴", AchievementCategory.Cardio, 100, 31),
        New("cardio_100km", "Distansmästaren",  "100 km totalt",                "🏅", AchievementCategory.Cardio, 400, 32),

        // Level
        New("level_5",  "Uppåt!",   "Nå nivå 5",  "⭐", AchievementCategory.Level, 0, 40),
        New("level_10", "Veteran",  "Nå nivå 10", "🌟", AchievementCategory.Level, 0, 41),
        New("level_25", "Legend",   "Nå nivå 25", "👑", AchievementCategory.Level, 0, 42),
    };

    private static Achievement New(string code, string title, string desc, string icon,
        AchievementCategory cat, int xp, int sort) => new()
    {
        Code = code,
        Title = title,
        Description = desc,
        Icon = icon,
        Category = cat,
        XpReward = xp,
        SortOrder = sort
    };
}
