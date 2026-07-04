using SQLite;

namespace FitnessQuest.Models;

/// <summary>
/// Single-row profile holding the user's daily nutrition goals.
/// </summary>
public class UserProfile
{
    [PrimaryKey]
    public int Id { get; set; } = 1;

    public string Name { get; set; } = "Athlete";

    public double WeightKg { get; set; } = 75;
    public double HeightCm { get; set; } = 178;
    public int Age { get; set; } = 30;
    public Sex Sex { get; set; } = Sex.Male;
    public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.Moderate;

    // Daily targets
    public int CalorieGoal { get; set; } = 2200;
    public int ProteinGoal { get; set; } = 150;
    public int CarbsGoal { get; set; } = 220;
    public int FatGoal { get; set; } = 70;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
