using System.Globalization;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using SmoothScrollModern.Scroll;

namespace SmoothScrollModern.Settings;

public sealed class ScrollProfile : ObservableObject
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _name = "Новый профиль";
    private ScrollSettings _scroll = new();

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value?.Trim() ?? string.Empty);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, string.IsNullOrWhiteSpace(value) ? "Новый профиль" : value.Trim());
    }

    public ScrollSettings Scroll
    {
        get => _scroll;
        set
        {
            var scroll = value ?? new ScrollSettings();
            scroll.Validate();
            if (SetProperty(ref _scroll, scroll))
            {
                OnScrollPropertiesChanged();
            }
        }
    }

    [JsonIgnore]
    public bool IsGlobal { get; set; }

    [JsonIgnore]
    public string ProfileSummaryText =>
        $"{FormatDecimal(Scroll.ScrollMultiplier)}x · {Scroll.DurationMs} мс · плавность {FormatDecimal(Scroll.Smoothness)} · ускорение {FormatDecimal(Scroll.Acceleration)}";

    [JsonIgnore]
    public double ProfileScrollMultiplier
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
            OnPropertyChanged(nameof(ProfileSummaryText));
        }
    }

    [JsonIgnore]
    public int ProfileDurationMs
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
            OnPropertyChanged(nameof(ProfileSummaryText));
        }
    }

    [JsonIgnore]
    public double ProfileSmoothness
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
            OnPropertyChanged(nameof(ProfileSummaryText));
        }
    }

    [JsonIgnore]
    public double ProfileAcceleration
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
            OnPropertyChanged(nameof(ProfileSummaryText));
        }
    }

    [JsonIgnore]
    public EasingType ProfileEasingType
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
    public bool ProfileEnableHorizontalScroll
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

    public void Validate()
    {
        if (!IsGlobal && string.IsNullOrWhiteSpace(Id))
        {
            Id = Guid.NewGuid().ToString("N");
        }

        Name = string.IsNullOrWhiteSpace(Name) ? "Новый профиль" : Name;
        Scroll ??= new ScrollSettings();
        Scroll.Validate();
    }

    private void OnScrollPropertiesChanged()
    {
        OnPropertyChanged(nameof(ProfileScrollMultiplier));
        OnPropertyChanged(nameof(ProfileDurationMs));
        OnPropertyChanged(nameof(ProfileSmoothness));
        OnPropertyChanged(nameof(ProfileAcceleration));
        OnPropertyChanged(nameof(ProfileEasingType));
        OnPropertyChanged(nameof(ProfileEnableHorizontalScroll));
        OnPropertyChanged(nameof(ProfileSummaryText));
    }

    private static string FormatDecimal(double value)
    {
        return value.ToString("0.###", CultureInfo.CurrentCulture);
    }
}
