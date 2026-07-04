using FitnessQuest.Models;

namespace FitnessQuest.Services;

/// <summary>
/// The outcome of registering an activity — used by the UI to fire off
/// celebratory feedback (XP toast, level-up dialog, unlocked badges).
/// </summary>
public class GamificationResult
{
    public int XpGained { get; set; }
    public bool LeveledUp { get; set; }
    public int OldLevel { get; set; }
    public int NewLevel { get; set; }
    public int StreakAfter { get; set; }
    public bool StreakIncreased { get; set; }
    public List<Achievement> NewAchievements { get; set; } = new();

    public bool HasCelebration => LeveledUp || NewAchievements.Count > 0;
}
