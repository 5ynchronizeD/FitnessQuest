using CommunityToolkit.Mvvm.Messaging;
using FitnessQuest.Services;
using ZXing.Net.Maui;

namespace FitnessQuest.Views;

public partial class BarcodeScanPage : ContentPage
{
    private bool _handled;

    public BarcodeScanPage()
    {
        InitializeComponent();
        Scanner.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.OneDimensional,
            AutoRotate = true,
            Multiple = false
        };
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_handled)
            return;

        var value = e.Results?.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(value))
            return;

        _handled = true;

        // Hop to the UI thread: send the result, then pop this page.
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            Scanner.IsDetecting = false;
            WeakReferenceMessenger.Default.Send(new BarcodeScannedMessage(value));
            await Shell.Current.GoToAsync("..");
        });
    }
}
