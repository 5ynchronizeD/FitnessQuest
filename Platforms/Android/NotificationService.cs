using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using FitnessQuest.Services;
using Application = Android.App.Application;

namespace FitnessQuest.Platforms.Android;

/// <summary>Android implementation of local notifications (channel + AlarmManager).</summary>
public class NotificationService : INotificationService
{
    public const string ChannelId = "fitnessquest_general";
    private const int DailyRequestCode = 5001;

    public Task InitializeAsync()
    {
        try
        {
            var ctx = Application.Context;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(ChannelId, "FitnessQuest",
                    NotificationImportance.Default)
                {
                    Description = "Påminnelser och vilotimer"
                };
                var mgr = (NotificationManager?)ctx.GetSystemService(Context.NotificationService);
                mgr?.CreateNotificationChannel(channel);
            }

            // Request POST_NOTIFICATIONS on Android 13+.
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu &&
                Platform.CurrentActivity is { } activity &&
                ContextCompat.CheckSelfPermission(ctx, "android.permission.POST_NOTIFICATIONS") != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(activity, new[] { "android.permission.POST_NOTIFICATIONS" }, 1001);
            }
        }
        catch { /* never let notification setup crash the app */ }
        return Task.CompletedTask;
    }

    public void ShowNow(int id, string title, string body)
    {
        try
        {
            var ctx = Application.Context;
            var builder = new NotificationCompat.Builder(ctx, ChannelId)
                .SetContentTitle(title)
                .SetContentText(body)
                .SetSmallIcon(ctx.ApplicationInfo!.Icon)
                .SetAutoCancel(true)
                .SetPriority((int)NotificationPriority.Default);
            NotificationManagerCompat.From(ctx).Notify(id, builder.Build());
        }
        catch { }
    }

    public void SetDailyReminder(bool enabled, int hour, int minute)
    {
        try
        {
            var ctx = Application.Context;
            var alarm = (AlarmManager?)ctx.GetSystemService(Context.AlarmService);
            if (alarm is null) return;

            var intent = new Intent(ctx, typeof(ReminderReceiver));
            var flags = PendingIntentFlags.UpdateCurrent;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S) flags |= PendingIntentFlags.Immutable;
            var pi = PendingIntent.GetBroadcast(ctx, DailyRequestCode, intent, flags);

            if (!enabled)
            {
                if (pi is not null) alarm.Cancel(pi);
                return;
            }

            var now = DateTime.Now;
            var first = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
            if (first <= now) first = first.AddDays(1);
            long triggerAt = (long)(first.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

            alarm.SetInexactRepeating(AlarmType.RtcWakeup, triggerAt, AlarmManager.IntervalDay, pi);
        }
        catch { }
    }
}

/// <summary>Fires the daily streak reminder notification.</summary>
[BroadcastReceiver(Enabled = true, Exported = false)]
public class ReminderReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        try
        {
            var ctx = context ?? Application.Context;
            var builder = new NotificationCompat.Builder(ctx, NotificationService.ChannelId)
                .SetContentTitle("Håll din streak vid liv 🔥")
                .SetContentText("Logga en måltid eller ett pass idag i FitnessQuest.")
                .SetSmallIcon(ctx.ApplicationInfo!.Icon)
                .SetAutoCancel(true)
                .SetPriority((int)NotificationPriority.Default);
            NotificationManagerCompat.From(ctx).Notify(9001, builder.Build());
        }
        catch { }
    }
}
