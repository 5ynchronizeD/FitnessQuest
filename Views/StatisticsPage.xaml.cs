using FitnessQuest.Drawing;
using FitnessQuest.ViewModels;
using Microsoft.Maui.Graphics;

namespace FitnessQuest.Views;

public partial class StatisticsPage : ContentPage
{
    private readonly StatisticsViewModel _vm;

    private readonly ChartDrawable _calorie = new()
    {
        BarColor = Color.FromArgb("#00E5A0"),
        ValueFormat = "0",
        ShowValues = false
    };
    private readonly ChartDrawable _volume = new() { BarColor = Color.FromArgb("#7C4DFF"), ShowValues = false };
    private readonly ChartDrawable _cardio = new() { BarColor = Color.FromArgb("#4ECDC4"), ValueFormat = "0.#", ShowValues = false };
    private readonly ChartDrawable _progression = new() { BarColor = Color.FromArgb("#FFC93C"), IsLine = true, ZeroBased = false };

    public StatisticsPage(StatisticsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

        CalorieChart.Drawable = _calorie;
        VolumeChart.Drawable = _volume;
        CardioChart.Drawable = _cardio;
        ProgressionChart.Drawable = _progression;

        _vm.ChartsUpdated += RefreshCharts;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private void RefreshCharts()
    {
        _calorie.Values = _vm.CalorieValues;
        _calorie.Labels = _vm.CalorieLabels;
        _calorie.GoalValue = _vm.CalorieGoal;

        _volume.Values = _vm.VolumeValues;
        _volume.Labels = _vm.VolumeLabels;

        _cardio.Values = _vm.CardioValues;
        _cardio.Labels = _vm.CardioLabels;

        _progression.Values = _vm.ProgressionValues;
        _progression.Labels = _vm.ProgressionLabels;

        CalorieChart.Invalidate();
        VolumeChart.Invalidate();
        CardioChart.Invalidate();
        ProgressionChart.Invalidate();
    }
}
