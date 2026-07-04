using System.Globalization;

namespace FitnessQuest.Converters;

/// <summary>true → 1.0, false → dimmed. Used to grey-out locked achievements.</summary>
public class BoolToOpacityConverter : IValueConverter
{
    public double TrueValue { get; set; } = 1.0;
    public double FalseValue { get; set; } = 0.35;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TrueValue : FalseValue;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Inverts a boolean.</summary>
public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;
}

/// <summary>value != null (and non-empty for strings) → true.</summary>
public class IsNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s ? !string.IsNullOrWhiteSpace(s) : value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>value == null (or empty for strings) → true.</summary>
public class IsNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s ? string.IsNullOrWhiteSpace(s) : value is null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Picks a colour depending on whether the achievement is unlocked.</summary>
public class BoolToColorConverter : IValueConverter
{
    public Color TrueColor { get; set; } = Colors.White;
    public Color FalseColor { get; set; } = Colors.Gray;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TrueColor : FalseColor;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
