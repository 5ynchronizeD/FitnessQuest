using SQLite;

namespace FitnessQuest.Models;

/// <summary>
/// A gym session. Structure: Workout 1—* <see cref="WorkoutExercise"/> 1—* <see cref="ExerciseSet"/>.
/// </summary>
public class Workout
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "Gympass";
    public string? Notes { get; set; }

    [Indexed]
    public DateTime PerformedAt { get; set; } = DateTime.Now;

    /// <summary>Elapsed workout time in seconds (0 if not tracked).</summary>
    public int DurationSeconds { get; set; }

    // Cached rollups for cheap list rendering.
    public int TotalSets { get; set; }
    public double TotalVolumeKg { get; set; }

    [Ignore] public string Summary => $"{TotalSets} set · {TotalVolumeKg:0} kg volym";
    [Ignore] public string DateDisplay => PerformedAt.ToString("ddd d MMM · HH:mm");
    [Ignore]
    public string DurationDisplay
    {
        get
        {
            if (DurationSeconds <= 0) return "–";
            var ts = TimeSpan.FromSeconds(DurationSeconds);
            return ts.Hours > 0 ? $"{ts.Hours}h {ts.Minutes}m" : $"{ts.Minutes}m";
        }
    }
}

/// <summary>
/// An exercise instance within a workout (e.g. "Bänkpress" as the 2nd movement).
/// Sets belong to this. Supersets are modelled by sharing <see cref="SupersetGroup"/>.
/// </summary>
public class WorkoutExercise
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int WorkoutId { get; set; }

    public string ExerciseName { get; set; } = string.Empty;
    public Equipment Equipment { get; set; } = Equipment.Barbell;

    public int OrderIndex { get; set; }

    /// <summary>Target rest between sets, in seconds.</summary>
    public int RestSeconds { get; set; } = 120;

    /// <summary>0 = standalone. Same non-zero value = grouped as a superset.</summary>
    public int SupersetGroup { get; set; }

    public string? Notes { get; set; }
}

/// <summary>A single set belonging to a <see cref="WorkoutExercise"/>.</summary>
public class ExerciseSet
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int WorkoutExerciseId { get; set; }

    /// <summary>Legacy/denormalised workout id, kept for direct rollup queries.</summary>
    [Indexed]
    public int WorkoutId { get; set; }

    public int SetNumber { get; set; }
    public int Reps { get; set; }
    public double WeightKg { get; set; }
    public SetType SetType { get; set; } = SetType.Normal;
    public bool IsCompleted { get; set; }

    /// <summary>Optional Rate of Perceived Exertion (0 = unset).</summary>
    public double Rpe { get; set; }

    [Ignore] public double Volume => Reps * WeightKg;
    [Ignore]
    public double EstimatedOneRepMax =>
        WeightKg > 0 && Reps > 0 ? WeightKg * (1 + Reps / 30.0) : 0; // Epley
}

/// <summary>
/// Catalog of exercises used for the picker + suggestions. Seeded on first
/// launch and extended when the user types a new exercise name.
/// </summary>
public class Exercise
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string Name { get; set; } = string.Empty;
    public string MuscleGroup { get; set; } = "Övrigt";
    public Equipment Equipment { get; set; } = Equipment.Barbell;
    public bool IsCustom { get; set; }
    public int UsageCount { get; set; }

    [Ignore] public string EquipmentLabel => Equipment.ToSwedish();
}

/// <summary>A cardio session: running, cycling or walking.</summary>
public class CardioSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public CardioType Type { get; set; }
    public double DistanceKm { get; set; }
    public double DurationMinutes { get; set; }
    public string? Notes { get; set; }

    [Indexed]
    public DateTime PerformedAt { get; set; } = DateTime.Now;

    [Ignore] public double PaceMinPerKm => DistanceKm > 0 ? DurationMinutes / DistanceKm : 0;
    [Ignore] public double SpeedKmh => DurationMinutes > 0 ? DistanceKm / (DurationMinutes / 60.0) : 0;

    [Ignore]
    public string PaceDisplay
    {
        get
        {
            if (PaceMinPerKm <= 0) return "–";
            int min = (int)PaceMinPerKm;
            int sec = (int)Math.Round((PaceMinPerKm - min) * 60);
            if (sec == 60) { min++; sec = 0; }
            return $"{min}:{sec:00} /km";
        }
    }

    [Ignore] public string Summary => $"{DistanceKm:0.0} km · {DurationMinutes:0} min · {PaceDisplay}";
    [Ignore] public string TypeName => Type.ToSwedish();
    [Ignore] public string TypeEmoji => Type.Emoji();
    [Ignore] public string DateDisplay => PerformedAt.ToString("ddd d MMM");
}
