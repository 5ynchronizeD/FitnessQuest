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
        New("streak_3",   "Igång!",           "Logga 3 dagar i rad",        "🔥", AchievementCategory.Streak, 50,  1),
        New("streak_7",   "En vecka stark",   "7 dagars streak",            "🔥", AchievementCategory.Streak, 120, 2),
        New("streak_14",  "Två veckor",       "14 dagars streak",           "🔥", AchievementCategory.Streak, 250, 3),
        New("streak_30",  "Obruten månad",    "30 dagars streak",           "🏆", AchievementCategory.Streak, 500, 4),
        New("streak_100", "Ostoppbar",        "100 dagars streak",          "💯", AchievementCategory.Streak, 2000, 5),

        // Nutrition
        New("food_first", "Första tuggan",    "Logga din första måltid",    "🍎", AchievementCategory.Nutrition, 20,  10),
        New("food_25",    "Matdagboksförare", "Logga 25 måltider",          "📒", AchievementCategory.Nutrition, 100, 11),
        New("food_100",   "Näringsnörd",      "Logga 100 måltider",         "🥗", AchievementCategory.Nutrition, 300, 12),
        New("food_365",   "Årets loggare",    "Logga 365 måltider",         "📚", AchievementCategory.Nutrition, 800, 13),

        // Gym
        New("gym_first", "Järnviljan",        "Slutför ditt första gympass", "🏋️", AchievementCategory.Gym, 40,  20),
        New("gym_10",    "Stamgäst",          "10 gympass",                  "💪", AchievementCategory.Gym, 150, 21),
        New("gym_25",    "Hängiven",          "25 gympass",                  "💪", AchievementCategory.Gym, 300, 22),
        New("gym_50",    "Gymråtta",          "50 gympass",                  "🐀", AchievementCategory.Gym, 600, 23),
        New("gym_vol",   "Tonvis",            "1 000 kg total volym i ett pass", "🏗️", AchievementCategory.Gym, 200, 24),
        New("gym_vol2",  "Kranförare",        "2 500 kg total volym i ett pass", "🏗️", AchievementCategory.Gym, 400, 25),

        // Cardio
        New("cardio_first", "Igång och rullar", "Logga ditt första cardiopass", "🏃", AchievementCategory.Cardio, 40,  30),
        New("cardio_10km",  "Milslukaren",      "10 km totalt",                 "🚴", AchievementCategory.Cardio, 100, 31),
        New("cardio_50km",  "Femtiofemman",     "50 km totalt",                 "🚴", AchievementCategory.Cardio, 250, 32),
        New("cardio_100km", "Distansmästaren",  "100 km totalt",                "🏅", AchievementCategory.Cardio, 400, 33),
        New("cardio_500km", "Ultralöparen",     "500 km totalt",                "🏅", AchievementCategory.Cardio, 1500, 34),

        // Level
        New("level_5",  "Uppåt!",   "Nå nivå 5",  "⭐", AchievementCategory.Level, 0, 40),
        New("level_10", "Veteran",  "Nå nivå 10", "🌟", AchievementCategory.Level, 0, 41),
        New("level_25", "Legend",   "Nå nivå 25", "👑", AchievementCategory.Level, 0, 42),
        New("level_50", "Halvsekel","Nå nivå 50", "🦄", AchievementCategory.Level, 0, 43),
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
