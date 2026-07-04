using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FitnessQuest.Services;

/// <summary>Broadcast when the scanner reads a barcode; carries the raw value.</summary>
public class BarcodeScannedMessage : ValueChangedMessage<string>
{
    public BarcodeScannedMessage(string value) : base(value) { }
}

/// <summary>Broadcast after any activity is logged so the dashboard can refresh.</summary>
public class DataChangedMessage : ValueChangedMessage<string>
{
    public DataChangedMessage(string area) : base(area) { }
}

/// <summary>Broadcast when an exercise is picked from the catalog for the editor.</summary>
public class ExercisePickedMessage : ValueChangedMessage<Models.Exercise>
{
    public ExercisePickedMessage(Models.Exercise value) : base(value) { }
}
