namespace FitnessQuest.Services;

/// <summary>Local notifications (rest-timer alert + daily streak reminder).</summary>
public interface INotificationService
{
    Task InitializeAsync();

    /// <summary>Post an immediate notification (e.g. rest finished).</summary>
    void ShowNow(int id, string title, string body);

    /// <summary>Enable/disable a daily reminder at the given local time.</summary>
    void SetDailyReminder(bool enabled, int hour, int minute);
}

/// <summary>No-op fallback so shared code can always resolve the service.</summary>
public class NoopNotificationService : INotificationService
{
    public Task InitializeAsync() => Task.CompletedTask;
    public void ShowNow(int id, string title, string body) { }
    public void SetDailyReminder(bool enabled, int hour, int minute) { }
}
