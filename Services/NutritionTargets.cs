using FitnessQuest.Models;

namespace FitnessQuest.Services;

public record MacroTargets(int Calories, int Protein, int Carbs, int Fat);

/// <summary>
/// Estimates daily calorie + macro targets from the profile using the
/// Mifflin-St Jeor BMR formula × activity factor (TDEE), then a standard
/// macro split (protein ~1.8 g/kg, fat 25% of kcal, rest carbs).
/// </summary>
public static class NutritionTargets
{
    public static double Bmr(UserProfile p)
    {
        // Mifflin-St Jeor
        double baseVal = 10 * p.WeightKg + 6.25 * p.HeightCm - 5 * p.Age;
        return p.Sex == Sex.Male ? baseVal + 5 : baseVal - 161;
    }

    public static int Tdee(UserProfile p) => (int)Math.Round(Bmr(p) * p.ActivityLevel.Factor());

    public static MacroTargets Suggest(UserProfile p)
    {
        int kcal = Tdee(p);
        int protein = (int)Math.Round(p.WeightKg * 1.8);
        int fat = (int)Math.Round(kcal * 0.25 / 9.0);
        int carbKcal = Math.Max(0, kcal - protein * 4 - fat * 9);
        int carbs = (int)Math.Round(carbKcal / 4.0);
        return new MacroTargets(kcal, protein, carbs, fat);
    }
}
