namespace FitnessQuest.Services;

/// <summary>A plate size and how many the user owns in total.</summary>
public record PlateSpec(double Weight, int OwnedCount);

/// <summary>One plate size and how many go on each side.</summary>
public record PlateResult(double Weight, int CountPerSide);

/// <summary>Full breakdown of how to load a bar to hit a target weight.</summary>
public class PlateSolution
{
    public double TargetWeight { get; set; }
    public double BarWeight { get; set; }
    public double AchievedWeight { get; set; }
    public List<PlateResult> PerSide { get; set; } = new();

    /// <summary>True if we ran out of a plate and could have gone closer with more.</summary>
    public bool LimitedByInventory { get; set; }

    public bool IsExact => Math.Abs(AchievedWeight - TargetWeight) < 0.01;
    public double Remainder => TargetWeight - AchievedWeight;

    /// <summary>e.g. "20 + 10 + 2.5" (per side, heaviest first).</summary>
    public string PerSideText =>
        PerSide.Count == 0
            ? "Bara stången"
            : string.Join(" + ", PerSide.SelectMany(p => Enumerable.Repeat(Fmt(p.Weight), p.CountPerSide)));

    private static string Fmt(double w) => w % 1 == 0 ? $"{w:0}" : $"{w:0.##}";
}

/// <summary>
/// Works out which plates to put on each side of a bar to reach a target total
/// weight, using a greedy largest-first fill. Respects how many of each plate
/// the user owns: a plate can be used at most floor(owned / 2) per side because
/// the bar is loaded symmetrically.
/// </summary>
public static class PlateCalculator
{
    public static readonly double[] DefaultPlates = { 25, 20, 15, 10, 5, 2.5, 1.25 };

    /// <summary>Simple overload assuming unlimited plates (kept for convenience).</summary>
    public static PlateSolution Solve(double targetWeight, double barWeight = 20, IEnumerable<double>? plates = null)
    {
        var specs = (plates ?? DefaultPlates).Select(p => new PlateSpec(p, int.MaxValue));
        return Solve(targetWeight, barWeight, specs);
    }

    public static PlateSolution Solve(double targetWeight, double barWeight, IEnumerable<PlateSpec> plates)
    {
        var available = plates
            .Where(p => p.Weight > 0 && p.OwnedCount > 0)
            .OrderByDescending(p => p.Weight)
            .ToArray();

        var solution = new PlateSolution { TargetWeight = targetWeight, BarWeight = barWeight };

        if (targetWeight <= barWeight)
        {
            solution.AchievedWeight = barWeight;
            return solution;
        }

        double perSide = (targetWeight - barWeight) / 2.0;
        double remaining = perSide;

        foreach (var plate in available)
        {
            int maxPerSide = plate.OwnedCount == int.MaxValue ? int.MaxValue : plate.OwnedCount / 2;
            if (maxPerSide <= 0) continue;

            int want = (int)Math.Floor(remaining / plate.Weight + 1e-9);
            int take = Math.Min(want, maxPerSide);
            if (take > 0)
            {
                solution.PerSide.Add(new PlateResult(plate.Weight, take));
                remaining -= take * plate.Weight;
            }
        }

        double loadedPerSide = perSide - remaining;
        solution.AchievedWeight = barWeight + loadedPerSide * 2;
        solution.LimitedByInventory = remaining > 0.01 && perSide > 0;
        return solution;
    }
}
