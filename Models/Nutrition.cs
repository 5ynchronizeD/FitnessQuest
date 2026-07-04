using SQLite;

namespace FitnessQuest.Models;

/// <summary>
/// A known food (from Open Food Facts, a barcode scan, or manual entry).
/// Doubles as the "recent + favourites" catalogue so re-logging is one tap.
/// Nutrition values are always per 100 g.
/// </summary>
public class FoodItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string? Barcode { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }

    public double KcalPer100g { get; set; }
    public double ProteinPer100g { get; set; }
    public double CarbsPer100g { get; set; }
    public double FatPer100g { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsFavorite { get; set; }
    public DateTime LastUsed { get; set; } = DateTime.Now;

    public string DisplayName => string.IsNullOrWhiteSpace(Brand) ? Name : $"{Name} · {Brand}";
    public string MacroSummary => $"{KcalPer100g:0} kcal · {ProteinPer100g:0}P {CarbsPer100g:0}K {FatPer100g:0}F / 100g";
}

/// <summary>
/// A single logged portion of food on a given day. Nutrition values are
/// denormalised (copied at log time) so past entries never change if the
/// underlying <see cref="FoodItem"/> is edited.
/// </summary>
public class FoodLogEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int FoodItemId { get; set; }

    public string Name { get; set; } = string.Empty;
    public double Grams { get; set; } = 100;
    public MealType Meal { get; set; }

    // Denormalised per-100g values captured at log time.
    public double KcalPer100g { get; set; }
    public double ProteinPer100g { get; set; }
    public double CarbsPer100g { get; set; }
    public double FatPer100g { get; set; }

    [Indexed]
    public DateTime LoggedAt { get; set; } = DateTime.Now;

    [Ignore] public double Kcal => KcalPer100g * Grams / 100.0;
    [Ignore] public double Protein => ProteinPer100g * Grams / 100.0;
    [Ignore] public double Carbs => CarbsPer100g * Grams / 100.0;
    [Ignore] public double Fat => FatPer100g * Grams / 100.0;

    [Ignore] public string Summary => $"{Grams:0} g · {Kcal:0} kcal";
    [Ignore] public string MealName => Meal.ToSwedish();
    [Ignore] public string MealEmoji => Meal.Emoji();
}
