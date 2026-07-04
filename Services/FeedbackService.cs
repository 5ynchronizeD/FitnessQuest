using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using FitnessQuest.Models;

namespace FitnessQuest.Services;

/// <summary>
/// Turns a <see cref="GamificationResult"/> into on-screen delight:
/// a quick XP toast, then dialogs for level-ups and unlocked badges.
/// This is the reward feedback that keeps logging addictive.
/// </summary>
public class FeedbackService
{
    public async Task CelebrateAsync(GamificationResult r)
    {
        // Fast, non-blocking XP toast.
        var msg = $"+{r.XpGained} XP";
        if (r.StreakIncreased && r.StreakAfter > 1)
            msg += $"  🔥 {r.StreakAfter} dagars streak!";

        try
        {
            await Toast.Make(msg, ToastDuration.Short, 16).Show();
        }
        catch
        {
            // Toast can fail in edge cases (e.g. no window); ignore silently.
        }

        // Level up dialog.
        if (r.LeveledUp)
        {
            await AlertAsync("🎉 LEVEL UP!",
                $"Du nådde nivå {r.NewLevel} – {LevelSystem.RankName(r.NewLevel)}!",
                "Nice!");
        }

        // Each unlocked achievement.
        foreach (var a in r.NewAchievements)
        {
            var reward = a.XpReward > 0 ? $"\n\n+{a.XpReward} XP bonus" : string.Empty;
            await AlertAsync($"{a.Icon}  Ny bragd!",
                $"{a.Title}\n{a.Description}{reward}",
                "Grymt!");
        }
    }

    private static async Task AlertAsync(string title, string message, string cancel)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is not null)
            await page.DisplayAlert(title, message, cancel);
    }
}
