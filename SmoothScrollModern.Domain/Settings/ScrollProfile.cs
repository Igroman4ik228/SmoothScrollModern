using System.Globalization;
using System.Text.Json.Serialization;
using SmoothScrollModern.Common;
using Windows.System;

namespace SmoothScrollModern.Settings;

public sealed class ScrollProfile : ObservableEntity
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
        $"{FormatDecimal(Scroll.DistanceMultiplier)}x · трение {FormatDecimal(Scroll.Friction)} · контроль {FormatDecimal(ProfileDirectionControlPercent)}%";

    [JsonIgnore]
    public IReadOnlyList<ShortcutKeyDisplay> ProfileBypassSmoothingKeyItems =>
        Scroll.BypassSmoothingVirtualKeys
            .Select(key => new ShortcutKeyDisplay(key, ShortcutKeys.Format(key)))
            .ToList();

    [JsonIgnore]
    public string ProfileBypassSmoothingKeysText => ProfileBypassSmoothingKeyItems.Count == 0
        ? "Не задано"
        : string.Join(", ", ProfileBypassSmoothingKeyItems.Select(key => key.Name));

    [JsonIgnore]
    public double ProfileScrollStrength
    {
        get => Scroll.DistanceMultiplier;
        set
        {
            if (Math.Abs(Scroll.DistanceMultiplier - value) < 0.0005)
            {
                return;
            }

            Scroll.DistanceMultiplier = value;
            Scroll.Validate();
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProfileSummaryText));
        }
    }

    [JsonIgnore]
    public double ProfileFriction
    {
        get => Scroll.Friction;
        set
        {
            if (Math.Abs(Scroll.Friction - value) < 0.0005)
            {
                return;
            }

            Scroll.Friction = value;
            Scroll.Validate();
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProfileSummaryText));
        }
    }

    [JsonIgnore]
    public double ProfileFastScrollBoostPercent
    {
        get => Scroll.BurstAcceleration * 100.0;
        set
        {
            if (Math.Abs(ProfileFastScrollBoostPercent - value) < 0.0005)
            {
                return;
            }

            Scroll.BurstAcceleration = value / 100.0;
            Scroll.Validate();
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProfileSummaryText));
        }
    }

    [JsonIgnore]
    public double ProfileMaxVelocity
    {
        get => Scroll.MaxVelocity;
        set
        {
            if (Math.Abs(Scroll.MaxVelocity - value) < 0.0005)
            {
                return;
            }

            Scroll.MaxVelocity = value;
            Scroll.Validate();
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public double ProfileStopVelocityThreshold
    {
        get => Scroll.StopVelocityThreshold;
        set
        {
            if (Math.Abs(Scroll.StopVelocityThreshold - value) < 0.0005)
            {
                return;
            }

            Scroll.StopVelocityThreshold = value;
            Scroll.Validate();
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public double ProfilePrecisionMultiplier
    {
        get => Scroll.PrecisionMultiplier;
        set
        {
            if (Math.Abs(Scroll.PrecisionMultiplier - value) < 0.0005)
            {
                return;
            }

            Scroll.PrecisionMultiplier = value;
            Scroll.Validate();
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public double ProfileDirectionControlPercent
    {
        get => (1.0 - Scroll.DirectionChangeDamping) * 100.0;
        set
        {
            if (Math.Abs(ProfileDirectionControlPercent - value) < 0.0005)
            {
                return;
            }

            Scroll.DirectionChangeDamping = 1.0 - (value / 100.0);
            Scroll.Validate();
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProfileSummaryText));
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

    public void AddProfileBypassSmoothingKey(VirtualKey virtualKey)
    {
        if (!ShortcutKeys.IsValid(virtualKey)
            || ShortcutKeys.ContainsConflict(Scroll.BypassSmoothingVirtualKeys, virtualKey))
        {
            NotifyBypassSmoothingKeysChanged();
            return;
        }

        Scroll.BypassSmoothingVirtualKeys.Add(virtualKey);
        Scroll.Validate();
        NotifyBypassSmoothingKeysChanged();
    }

    public void RemoveProfileBypassSmoothingKey(VirtualKey virtualKey)
    {
        if (!Scroll.BypassSmoothingVirtualKeys.Remove(virtualKey))
        {
            return;
        }

        NotifyBypassSmoothingKeysChanged();
    }

    private void OnScrollPropertiesChanged()
    {
        OnPropertyChanged(nameof(ProfileScrollStrength));
        OnPropertyChanged(nameof(ProfileFriction));
        OnPropertyChanged(nameof(ProfileFastScrollBoostPercent));
        OnPropertyChanged(nameof(ProfileMaxVelocity));
        OnPropertyChanged(nameof(ProfileStopVelocityThreshold));
        OnPropertyChanged(nameof(ProfilePrecisionMultiplier));
        OnPropertyChanged(nameof(ProfileDirectionControlPercent));
        OnPropertyChanged(nameof(ProfileEnableHorizontalScroll));
        NotifyBypassSmoothingKeysChanged();
        OnPropertyChanged(nameof(ProfileSummaryText));
    }

    private void NotifyBypassSmoothingKeysChanged()
    {
        OnPropertyChanged(nameof(ProfileBypassSmoothingKeyItems));
        OnPropertyChanged(nameof(ProfileBypassSmoothingKeysText));
    }

    private static string FormatDecimal(double value)
    {
        return value.ToString("0.###", CultureInfo.CurrentCulture);
    }

}

public sealed record ShortcutKeyDisplay(VirtualKey VirtualKey, string Name);
