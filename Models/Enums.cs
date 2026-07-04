namespace FitnessQuest.Models;

public enum MealType
{
    Breakfast,
    Lunch,
    Dinner,
    Snack
}

public enum CardioType
{
    Running,
    Cycling,
    Walking
}

public enum ActivityType
{
    FoodLog,
    GymWorkout,
    CardioSession
}

public enum AchievementCategory
{
    Streak,
    Nutrition,
    Gym,
    Cardio,
    Level
}

public enum Sex
{
    Male,
    Female
}

/// <summary>Activity multiplier for TDEE (Mifflin-St Jeor).</summary>
public enum ActivityLevel
{
    Sedentary,
    Light,
    Moderate,
    Active,
    VeryActive
}

public enum Equipment
{
    Barbell,
    Dumbbell,
    Machine,
    Cable,
    Bodyweight,
    Kettlebell,
    Other
}

/// <summary>Type of a single set (Strong-style).</summary>
public enum SetType
{
    Normal,
    Warmup,
    Dropset,
    Failure
}

public static class EnumExtensions
{
    public static string ToSwedish(this MealType meal) => meal switch
    {
        MealType.Breakfast => "Frukost",
        MealType.Lunch => "Lunch",
        MealType.Dinner => "Middag",
        MealType.Snack => "Mellanmål",
        _ => meal.ToString()
    };

    public static string ToSwedish(this CardioType type) => type switch
    {
        CardioType.Running => "Löpning",
        CardioType.Cycling => "Cykling",
        CardioType.Walking => "Promenad",
        _ => type.ToString()
    };

    public static string Emoji(this CardioType type) => type switch
    {
        CardioType.Running => "🏃",
        CardioType.Cycling => "🚴",
        CardioType.Walking => "🚶",
        _ => "🏃"
    };

    public static string Emoji(this MealType meal) => meal switch
    {
        MealType.Breakfast => "🌅",
        MealType.Lunch => "🍽️",
        MealType.Dinner => "🌙",
        MealType.Snack => "🍎",
        _ => "🍽️"
    };

    public static string ToSwedish(this Equipment eq) => eq switch
    {
        Equipment.Barbell => "Skivstång",
        Equipment.Dumbbell => "Hantlar",
        Equipment.Machine => "Maskin",
        Equipment.Cable => "Kabel",
        Equipment.Bodyweight => "Kroppsvikt",
        Equipment.Kettlebell => "Kettlebell",
        _ => "Övrigt"
    };

    public static string ShortLabel(this SetType t) => t switch
    {
        SetType.Warmup => "W",
        SetType.Dropset => "D",
        SetType.Failure => "F",
        _ => string.Empty
    };

    public static string ToSwedish(this SetType t) => t switch
    {
        SetType.Warmup => "Uppvärmning",
        SetType.Dropset => "Dropset",
        SetType.Failure => "Till failure",
        _ => "Normalt set"
    };

    public static string ToSwedish(this ActivityLevel a) => a switch
    {
        ActivityLevel.Sedentary => "Stillasittande",
        ActivityLevel.Light => "Lätt aktiv",
        ActivityLevel.Moderate => "Måttligt aktiv",
        ActivityLevel.Active => "Aktiv",
        ActivityLevel.VeryActive => "Mycket aktiv",
        _ => a.ToString()
    };

    public static double Factor(this ActivityLevel a) => a switch
    {
        ActivityLevel.Sedentary => 1.2,
        ActivityLevel.Light => 1.375,
        ActivityLevel.Moderate => 1.55,
        ActivityLevel.Active => 1.725,
        ActivityLevel.VeryActive => 1.9,
        _ => 1.2
    };

    public static string ToSwedish(this Sex s) => s == Sex.Male ? "Man" : "Kvinna";
}
