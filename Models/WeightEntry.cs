using SQLite;

namespace FitnessQuest.Models;

/// <summary>A logged body-weight measurement.</summary>
public class WeightEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public double WeightKg { get; set; }

    [Indexed]
    public DateTime LoggedAt { get; set; } = DateTime.Now;

    [Ignore] public string DateDisplay => LoggedAt.ToString("ddd d MMM");
}
