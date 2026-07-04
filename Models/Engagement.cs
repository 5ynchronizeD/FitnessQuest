using SQLite;

namespace FitnessQuest.Models;

/// <summary>Water intake for one day (one row per day). A glass = 250 ml.</summary>
public class WaterDay
{
    [PrimaryKey]
    public string DateKey { get; set; } = string.Empty; // yyyy-MM-dd
    public int Glasses { get; set; }
}

/// <summary>A daily challenge/quest instance for a specific day.</summary>
public class DailyChallenge
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string DateKey { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Target { get; set; }
    public int XpReward { get; set; }
    public bool Claimed { get; set; }

    // Progress is computed at runtime, not stored.
    [Ignore] public int Progress { get; set; }
    [Ignore] public bool Completed => Progress >= Target;
    [Ignore] public string ProgressText => $"{Math.Min(Progress, Target)}/{Target}";
    [Ignore] public double ProgressFraction => Target <= 0 ? 0 : Math.Min(1.0, (double)Progress / Target);
    [Ignore] public string XpText => $"+{XpReward} XP";
}
