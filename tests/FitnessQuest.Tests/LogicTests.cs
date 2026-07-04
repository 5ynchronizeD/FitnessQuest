using FitnessQuest.Models;
using FitnessQuest.Services;
using Xunit;

namespace FitnessQuest.Tests;

public class PlateCalculatorTests
{
    [Fact]
    public void Solve_ExactWithUnlimitedPlates()
    {
        var sol = PlateCalculator.Solve(60, 20);
        Assert.True(sol.IsExact);
        Assert.Equal(60, sol.AchievedWeight, 3);
        Assert.Contains(sol.PerSide, p => p.Weight == 20 && p.CountPerSide == 1);
    }

    [Fact]
    public void Solve_SkipsUnavailablePlate()
    {
        // No 20 kg plates → 60 kg must be reached with 15 + 5 per side.
        var specs = new[]
        {
            new PlateSpec(25, 0), new PlateSpec(20, 0), new PlateSpec(15, 2),
            new PlateSpec(10, 2), new PlateSpec(5, 2), new PlateSpec(2.5, 2)
        };
        var sol = PlateCalculator.Solve(60, 20, specs);
        Assert.True(sol.IsExact);
        Assert.Contains(sol.PerSide, p => p.Weight == 15);
        Assert.Contains(sol.PerSide, p => p.Weight == 5);
    }

    [Fact]
    public void Solve_RespectsCountAndFlagsInventoryLimit()
    {
        // Only two 20 kg plates (one per side max) → can't reach 100 kg.
        var specs = new[] { new PlateSpec(20, 2) };
        var sol = PlateCalculator.Solve(100, 20, specs);
        Assert.Equal(60, sol.AchievedWeight, 3); // 20 bar + one 20 per side
        Assert.True(sol.LimitedByInventory);
    }
}

public class NutritionTargetsTests
{
    [Fact]
    public void Suggest_MatchesMifflinStJeor()
    {
        var p = new UserProfile { WeightKg = 75, HeightCm = 178, Age = 30, Sex = Sex.Male, ActivityLevel = ActivityLevel.Moderate };
        var t = NutritionTargets.Suggest(p);
        Assert.Equal(2662, t.Calories);
        Assert.Equal(135, t.Protein);   // 75 * 1.8
        Assert.Equal(74, t.Fat);        // 25% of kcal / 9
        Assert.True(t.Carbs > 350 && t.Carbs < 375);
    }
}

public class LevelSystemTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 100)]
    [InlineData(3, 300)]
    [InlineData(5, 1000)]
    public void XpForLevel_FollowsCurve(int level, int expected)
        => Assert.Equal(expected, LevelSystem.XpForLevel(level));

    [Theory]
    [InlineData(0, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 2)]
    [InlineData(299, 2)]
    [InlineData(300, 3)]
    public void LevelForXp_IsInverse(int xp, int expectedLevel)
        => Assert.Equal(expectedLevel, LevelSystem.LevelForXp(xp));
}

public class NutritionAdvisorTests
{
    private static UserProfile Goals() => new() { CalorieGoal = 2000, ProteinGoal = 150 };

    [Fact]
    public void QuietMidday_WhenNothingLogged()
        => Assert.Null(NutritionAdvisor.Suggest(0, 0, 0, 0, Goals(), 12, 0));

    [Fact]
    public void FlagsOverCalories()
    {
        var tip = NutritionAdvisor.Suggest(2500, 100, 300, 80, Goals(), 15, 4);
        Assert.NotNull(tip);
        Assert.Contains("över", tip!);
    }

    [Fact]
    public void FlagsLowProteinInEvening()
    {
        var tip = NutritionAdvisor.Suggest(1500, 40, 200, 50, Goals(), 19, 3);
        Assert.NotNull(tip);
        Assert.Contains("protein", tip!);
    }
}

public class WorkoutImportServiceTests
{
    private const string Tcx = @"<?xml version=""1.0""?>
<TrainingCenterDatabase xmlns=""http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2"">
 <Activities><Activity Sport=""Running""><Lap><Track>
  <Trackpoint><Time>2026-07-04T08:00:00Z</Time><DistanceMeters>0</DistanceMeters><AltitudeMeters>10</AltitudeMeters><HeartRateBpm><Value>120</Value></HeartRateBpm></Trackpoint>
  <Trackpoint><Time>2026-07-04T08:05:00Z</Time><DistanceMeters>1000</DistanceMeters><AltitudeMeters>20</AltitudeMeters><HeartRateBpm><Value>160</Value></HeartRateBpm></Trackpoint>
 </Track></Lap></Activity></Activities>
</TrainingCenterDatabase>";

    [Fact]
    public void ParsesTcx()
    {
        var w = new WorkoutImportService().Parse(Tcx);
        Assert.NotNull(w);
        Assert.Equal(CardioType.Running, w!.Type);
        Assert.Equal(1.0, w.DistanceKm, 2);
        Assert.Equal(5.0, w.DurationMinutes, 1);
        Assert.Equal(140, w.AvgHeartRate);
        Assert.Equal(160, w.MaxHeartRate);
        Assert.Equal(10, w.ElevationGainM, 1);
    }

    [Fact]
    public void ReturnsNullForGarbage()
        => Assert.Null(new WorkoutImportService().Parse("not xml at all"));
}
