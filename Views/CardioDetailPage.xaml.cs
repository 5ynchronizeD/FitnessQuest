using FitnessQuest.Drawing;
using FitnessQuest.ViewModels;
using Microsoft.Maui.Graphics;

namespace FitnessQuest.Views;

public partial class CardioDetailPage : ContentPage
{
    private readonly CardioDetailViewModel _vm;

    private readonly ChartDrawable _hr = new() { BarColor = Color.FromArgb("#FF6B6B"), IsLine = true };
    private readonly ChartDrawable _ele = new() { BarColor = Color.FromArgb("#FFD166"), IsLine = true };
    private readonly ChartDrawable _paceHr = new() { BarColor = Color.FromArgb("#FF6B6B"), ShowValues = true, ValueFormat = "0" };

    public CardioDetailPage(CardioDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

        HrChart.Drawable = _hr;
        EleChart.Drawable = _ele;
        PaceHrChart.Drawable = _paceHr;

        _vm.ChartsUpdated += RefreshCharts;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
    }

    private void RefreshCharts()
    {
        _hr.Values = _vm.HrValues;
        _hr.Labels = _vm.HrLabels;
        _ele.Values = _vm.EleValues;
        _ele.Labels = _vm.EleLabels;
        _paceHr.Values = _vm.PaceHrValues;
        _paceHr.Labels = _vm.PaceHrLabels;

        HrChart.Invalidate();
        EleChart.Invalidate();
        PaceHrChart.Invalidate();
    }
}
