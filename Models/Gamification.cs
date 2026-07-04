using SQLite;

namespace FitnessQuest.Models;

/// <summary>
/// Single-row persistent state for the gamification engine: XP, streaks.
/// Level is derived from <see cref="TotalXp"/> via <see cref="LevelSystem"/>.
/// </summary>
public class GamificationState
{
    [PrimaryKey]
    public int Id { get; set; } = 1;

    public int TotalXp { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }

    /// <summary>Date (date-only semantics) of the most recent logged activity.</summary>
    public DateTime? LastActivityDate { get; set; }

    [Ignore] public int Level => LevelSystem.LevelForXp(TotalXp);
    [Ignore] public int XpIntoLevel => TotalXp - LevelSystem.XpForLevel(Level);
    [Ignore] public int XpForNextLevel => LevelSystem.XpForLevel(Level + 1) - LevelSystem.XpForLevel(Level);
    [Ignore] public double LevelProgress => XpForNextLevel == 0 ? 0 : (double)XpIntoLevel / XpForNextLevel;
    [Ignore] public string Rank => LevelSystem.RankName(Level);
}

/// <summary>
/// A badge the user can unlock. Rows are seeded on first launch and then
/// flipped to unlocked as the user hits thresholds.
/// </summary>
public class Achievement
{
    [PrimaryKey]
    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "🏅";
    public AchievementCategory Category { get; set; }

    public int XpReward { get; set; }
    public bool IsUnlocked { get; set; }
    public DateTime? UnlockedAt { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>
/// Central level curve and rank naming. XP needed to *reach* level L is
/// 50 * (L-1) * L, giving a gently accelerating curve
/// (L2=100, L3=300, L4=600, L5=1000, ...).
/// </summary>
public static class LevelSystem
{
    public static int XpForLevel(int level)
    {
        if (level < 1) level = 1;
        return 50 * (level - 1) * level;
    }

    public static int LevelForXp(int totalXp)
    {
        int level = 1;
        while (XpForLevel(level + 1) <= totalXp)
            level++;
        return level;
    }

    public static string RankName(int level) => level switch
    {
        < 3 => "Nybörjare",
        < 6 => "Motiverad",
        < 10 => "Dedikerad",
        < 15 => "Atlet",
        < 25 => "Elit",
        _ => "Legend"
    };
}
