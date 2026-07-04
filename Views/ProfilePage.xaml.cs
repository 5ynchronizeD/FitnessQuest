using FitnessQuest.Drawing;
using FitnessQuest.ViewModels;
using Microsoft.Maui.Graphics;

namespace FitnessQuest.Views;

public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel _vm;
    private readonly ChartDrawable _weight = new() { BarColor = Color.FromArgb("#00E5A0"), IsLine = true, ZeroBased = false };

    public ProfilePage(ProfileViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        WeightChart.Drawable = _weight;
        _vm.ChartUpdated += RefreshChart;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
    }

    private void RefreshChart()
    {
        _weight.Values = _vm.WeightValues;
        _weight.Labels = _vm.WeightLabels;
        WeightChart.Invalidate();
    }
}
