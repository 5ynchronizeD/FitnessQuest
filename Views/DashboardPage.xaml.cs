using FitnessQuest.Services;
using FitnessQuest.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessQuest.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _vm;
    private readonly IServiceProvider _services;
    private bool _checkedOnboarding;
    private bool _initedNotifications;

    public DashboardPage(DashboardViewModel vm, IServiceProvider services)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _services = services;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadCommand.Execute(null);

        if (!_initedNotifications)
        {
            _initedNotifications = true;
            var notifications = _services.GetRequiredService<INotificationService>();
            await notifications.InitializeAsync();
            // Re-apply the saved daily-reminder setting on each launch.
            if (Preferences.Get("reminder_enabled", false))
                notifications.SetDailyReminder(true, Preferences.Get("reminder_hour", 20), 0);
        }

        if (!_checkedOnboarding && !Preferences.Get(OnboardingViewModel.OnboardedKey, false))
        {
            _checkedOnboarding = true;
            var page = _services.GetRequiredService<OnboardingPage>();
            await Navigation.PushModalAsync(page);
        }
    }
}
