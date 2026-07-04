using FitnessQuest.Models;

namespace FitnessQuest.Services;

/// <summary>
/// Gives at most one short, timely nutrition tip — and only when it's actually
/// useful (never early in the day, never on an empty log). Deliberately quiet:
/// returns null most of the time.
/// </summary>
public static class NutritionAdvisor
{
    /// <summary>
    /// Returns a single suggestion, or null if nothing is worth saying right now.
    /// </summary>
    public static string? Suggest(
        double kcal, double protein, double carbs, double fat,
        UserProfile goals, int hour, int entryCount)
    {
        // Nothing logged yet → don't nag at all until it's clearly evening.
        if (entryCount == 0)
        {
            if (hour >= 20)
                return "🍽️ Inget loggat idag än – glöm inte att äta och logga.";
            return null;
        }

        // Already over the calorie goal — worth flagging whenever it happens.
        if (goals.CalorieGoal > 0 && kcal > goals.CalorieGoal * 1.05)
        {
            int over = (int)Math.Round(kcal - goals.CalorieGoal);
            return $"⚠️ Du är {over} kcal över dagens mål – ta det lugnt med resten.";
        }

        // Protein lagging relative to how far into the day we are.
        if (goals.ProteinGoal > 0 && hour >= 14)
        {
            double expected = goals.ProteinGoal * DayFraction(hour);
            if (protein < expected * 0.8)
            {
                int remaining = Math.Max(0, (int)Math.Round(goals.ProteinGoal - protein));
                if (remaining >= 20)
                    return $"💪 Lite lågt på protein – {remaining} g kvar till målet. En proteinkälla till hjälper.";
            }
        }

        // Under-eating late in the day.
        if (goals.CalorieGoal > 0 && hour >= 20 && kcal < goals.CalorieGoal * 0.6)
        {
            int remaining = (int)Math.Round(goals.CalorieGoal - kcal);
            return $"🍽️ Du har ätit få kalorier idag – ca {remaining} kcal kvar till målet.";
        }

        return null;
    }

    /// <summary>Rough fraction of daily intake expected by a given hour (07→0, 21→1).</summary>
    private static double DayFraction(int hour)
    {
        double f = (hour - 7) / 14.0;
        return Math.Max(0, Math.Min(1, f));
    }
}
