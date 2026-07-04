using SQLite;

namespace FitnessQuest.Models;

/// <summary>
/// A saved workout routine. The exercise/set structure is stored as JSON in
/// <see cref="PayloadJson"/> (a list of <see cref="TemplateExercise"/>).
/// </summary>
public class WorkoutTemplate
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "Rutin";
    public string PayloadJson { get; set; } = "[]";
    public int ExerciseCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastUsed { get; set; } = DateTime.Now;

    [Ignore] public string Summary => $"{ExerciseCount} övningar";
}

/// <summary>One exercise inside a template (serialized, not a table).</summary>
public class TemplateExercise
{
    public string ExerciseName { get; set; } = string.Empty;
    public Equipment Equipment { get; set; }
    public int RestSeconds { get; set; } = 120;
    public int SupersetGroup { get; set; }
    public List<TemplateSet> Sets { get; set; } = new();
}

/// <summary>One planned set inside a template exercise.</summary>
public class TemplateSet
{
    public double WeightKg { get; set; }
    public int Reps { get; set; }
    public SetType SetType { get; set; }
}
