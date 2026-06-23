using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Globalization.NumberFormatting;

namespace SmoothScrollModern.Features.Profiles.Controls;

public sealed partial class ProfileSettingRow : UserControl
{
    private bool _isNormalizingValue;

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(ProfileSettingRow),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description),
        typeof(string),
        typeof(ProfileSettingRow),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum),
        typeof(double),
        typeof(ProfileSettingRow),
        new PropertyMetadata(0d));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum),
        typeof(double),
        typeof(ProfileSettingRow),
        new PropertyMetadata(1d));

    public static readonly DependencyProperty StepFrequencyProperty = DependencyProperty.Register(
        nameof(StepFrequency),
        typeof(double),
        typeof(ProfileSettingRow),
        new PropertyMetadata(1d, OnNumericSettingsChanged));

    public static readonly DependencyProperty SmallChangeProperty = DependencyProperty.Register(
        nameof(SmallChange),
        typeof(double),
        typeof(ProfileSettingRow),
        new PropertyMetadata(1d));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(double),
        typeof(ProfileSettingRow),
        new PropertyMetadata(0d, OnValueChanged));

    public static readonly DependencyProperty FractionDigitsProperty = DependencyProperty.Register(
        nameof(FractionDigits),
        typeof(int),
        typeof(ProfileSettingRow),
        new PropertyMetadata(0, OnNumericSettingsChanged));

    public static readonly DependencyProperty NumberFormatterProperty = DependencyProperty.Register(
        nameof(NumberFormatter),
        typeof(INumberFormatter2),
        typeof(ProfileSettingRow),
        new PropertyMetadata(null, OnNumberFormatterChanged));

    public ProfileSettingRow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double StepFrequency
    {
        get => (double)GetValue(StepFrequencyProperty);
        set => SetValue(StepFrequencyProperty, value);
    }

    public double SmallChange
    {
        get => (double)GetValue(SmallChangeProperty);
        set => SetValue(SmallChangeProperty, value);
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int FractionDigits
    {
        get => (int)GetValue(FractionDigitsProperty);
        set => SetValue(FractionDigitsProperty, value);
    }

    public INumberFormatter2? NumberFormatter
    {
        get => (INumberFormatter2?)GetValue(NumberFormatterProperty);
        set => SetValue(NumberFormatterProperty, value);
    }

    private static void OnNumberFormatterChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not ProfileSettingRow row)
        {
            return;
        }

        if (args.NewValue is INumberFormatter2 formatter)
        {
            row.ValueBox.NumberFormatter = formatter;
            return;
        }

        row.UpdateNumberFormatter();
    }

    private static void OnNumericSettingsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is ProfileSettingRow row)
        {
            row.UpdateNumberFormatter();
            row.CoerceValue();
        }
    }

    private static void OnValueChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is ProfileSettingRow row)
        {
            row.CoerceValue();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateNumberFormatter();
        CoerceValue();
    }

    private void OnValueBoxValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        CoerceValue(args.NewValue);
    }

    private void UpdateNumberFormatter()
    {
        if (NumberFormatter is not null)
        {
            ValueBox.NumberFormatter = NumberFormatter;
            return;
        }

        var digits = Math.Clamp(FractionDigits, 0, 3);
        var increment = digits == 0 ? 1d : Math.Pow(10, -digits);

        ValueBox.NumberFormatter = new DecimalFormatter
        {
            FractionDigits = 0,
            NumberRounder = new IncrementNumberRounder
            {
                Increment = increment,
                RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp
            }
        };
    }

    private void CoerceValue()
    {
        CoerceValue(Value);
    }

    private void CoerceValue(double candidateValue)
    {
        if (_isNormalizingValue)
        {
            return;
        }

        var normalizedValue = NormalizeValue(candidateValue);
        if (AreClose(Value, normalizedValue)
            && !double.IsNaN(ValueBox.Value)
            && AreClose(ValueBox.Value, normalizedValue))
        {
            return;
        }

        _isNormalizingValue = true;
        try
        {
            SetValue(ValueProperty, normalizedValue);
            if (double.IsNaN(ValueBox.Value) || !AreClose(ValueBox.Value, normalizedValue))
            {
                ValueBox.Value = normalizedValue;
            }
        }
        finally
        {
            _isNormalizingValue = false;
        }
    }

    private double NormalizeValue(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            value = Minimum;
        }

        var normalizedValue = Math.Clamp(value, Minimum, Maximum);
        if (StepFrequency > 0)
        {
            var steps = Math.Round((normalizedValue - Minimum) / StepFrequency, MidpointRounding.AwayFromZero);
            normalizedValue = Minimum + (steps * StepFrequency);
        }

        var digits = Math.Clamp(FractionDigits, 0, 3);
        normalizedValue = Math.Round(normalizedValue, digits, MidpointRounding.AwayFromZero);
        return Math.Clamp(normalizedValue, Minimum, Maximum);
    }

    private static bool AreClose(double left, double right)
    {
        return Math.Abs(left - right) < 0.0005;
    }
}
