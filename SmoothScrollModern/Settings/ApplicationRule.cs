using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using SmoothScrollModern.Scroll;

namespace SmoothScrollModern.Settings;

public sealed class ApplicationRule : INotifyPropertyChanged
{
    private const string UnknownProcessName = "unknown.exe";
    private const string UnknownDisplayName = "Неизвестное приложение";

    private string _processName = string.Empty;
    private string _executablePath = string.Empty;
    private string _displayName = string.Empty;
    private string _scrollProfileId = string.Empty;
    private ScrollSettings _scroll = new();
    private ScrollDeliveryMode _deliveryMode = ScrollDeliveryMode.FineDelta;
    private bool _isSmoothScrollDisabled = true;
    private bool _isUserRule = true;
    private bool _isRuleEnabled = true;
    private bool _useCustomScrollSettings;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ProcessName
    {
        get => _processName;
        set
        {
            if (SetField(ref _processName, NormalizeProcessName(value)))
            {
                OnPropertyChanged(nameof(ProcessNameText));
                OnPropertyChanged(nameof(DisplayNameText));
            }
        }
    }

    public string ExecutablePath
    {
        get => _executablePath;
        set
        {
            if (SetField(ref _executablePath, NormalizeExecutablePath(value)))
            {
                OnPropertyChanged(nameof(ExecutablePathText));
                OnPropertyChanged(nameof(HasExecutablePath));
            }
        }
    }

    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (SetField(ref _displayName, value?.Trim() ?? string.Empty))
            {
                OnPropertyChanged(nameof(DisplayNameText));
            }
        }
    }

    public string ScrollProfileId
    {
        get => _scrollProfileId;
        set => SetField(ref _scrollProfileId, value?.Trim() ?? string.Empty);
    }

    public bool IsSmoothScrollDisabled
    {
        get => _isSmoothScrollDisabled;
        set => SetField(ref _isSmoothScrollDisabled, value);
    }

    public bool IsUserRule
    {
        get => _isUserRule;
        set => SetField(ref _isUserRule, value);
    }

    public bool IsRuleEnabled
    {
        get => _isRuleEnabled;
        set => SetField(ref _isRuleEnabled, value);
    }

    public bool UseCustomScrollSettings
    {
        get => _useCustomScrollSettings;
        set => SetField(ref _useCustomScrollSettings, value);
    }

    public ScrollDeliveryMode DeliveryMode
    {
        get => _deliveryMode;
        set => SetField(ref _deliveryMode, value);
    }

    public ScrollSettings Scroll
    {
        get => _scroll;
        set
        {
            _scroll = value ?? new ScrollSettings();
            _scroll.Validate();
            OnPropertyChanged();
            OnScrollPropertiesChanged();
        }
    }

    [JsonIgnore]
    public double RuleScrollMultiplier
    {
        get => Scroll.ScrollMultiplier;
        set
        {
            if (Math.Abs(Scroll.ScrollMultiplier - value) < 0.0005)
            {
                return;
            }

            Scroll.ScrollMultiplier = value;
            Scroll.Validate();
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public int RuleDurationMs
    {
        get => Scroll.DurationMs;
        set
        {
            if (Scroll.DurationMs == value)
            {
                return;
            }

            Scroll.DurationMs = value;
            Scroll.Validate();
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public double RuleSmoothness
    {
        get => Scroll.Smoothness;
        set
        {
            if (Math.Abs(Scroll.Smoothness - value) < 0.0005)
            {
                return;
            }

            Scroll.Smoothness = value;
            Scroll.Validate();
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public double RuleAcceleration
    {
        get => Scroll.Acceleration;
        set
        {
            if (Math.Abs(Scroll.Acceleration - value) < 0.0005)
            {
                return;
            }

            Scroll.Acceleration = value;
            Scroll.Validate();
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public EasingType RuleEasingType
    {
        get => Scroll.EasingType;
        set
        {
            if (Scroll.EasingType == value)
            {
                return;
            }

            Scroll.EasingType = value;
            Scroll.Validate();
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public bool RuleEnableHorizontalScroll
    {
        get => Scroll.EnableHorizontalScroll;
        set
        {
            if (Scroll.EnableHorizontalScroll == value)
            {
                return;
            }

            Scroll.EnableHorizontalScroll = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public string DisplayNameText => string.IsNullOrWhiteSpace(DisplayName)
        ? UnknownDisplayName
        : DisplayName;

    [JsonIgnore]
    public string ProcessNameText => string.IsNullOrWhiteSpace(ProcessName)
        ? UnknownProcessName
        : ProcessName;

    [JsonIgnore]
    public string ExecutablePathText => string.IsNullOrWhiteSpace(ExecutablePath)
        ? string.Empty
        : ExecutablePath;

    [JsonIgnore]
    public bool HasExecutablePath => !string.IsNullOrWhiteSpace(ExecutablePath);

    public void Validate()
    {
        ProcessName = NormalizeProcessName(ProcessName);
        ExecutablePath = NormalizeExecutablePath(ExecutablePath);
        ScrollProfileId = ScrollProfileId?.Trim() ?? string.Empty;
        DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? ProcessNameText : DisplayName;
        Scroll ??= new ScrollSettings();
        Scroll.Validate();

        if (!Enum.IsDefined(DeliveryMode))
        {
            DeliveryMode = ScrollDeliveryMode.FineDelta;
        }
    }

    public static string NormalizeProcessName(string processName)
    {
        var normalized = processName?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return UnknownProcessName;
        }

        return normalized.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : $"{normalized}.exe";
    }

    public static string NormalizeExecutablePath(string executablePath)
    {
        return executablePath?.Trim() ?? string.Empty;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnScrollPropertiesChanged()
    {
        OnPropertyChanged(nameof(RuleScrollMultiplier));
        OnPropertyChanged(nameof(RuleDurationMs));
        OnPropertyChanged(nameof(RuleSmoothness));
        OnPropertyChanged(nameof(RuleAcceleration));
        OnPropertyChanged(nameof(RuleEasingType));
        OnPropertyChanged(nameof(RuleEnableHorizontalScroll));
    }
}
