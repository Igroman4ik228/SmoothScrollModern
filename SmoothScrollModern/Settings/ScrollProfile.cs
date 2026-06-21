using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using SmoothScrollModern.Scroll;

namespace SmoothScrollModern.Settings;

public sealed class ScrollProfile : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _name = "Новый профиль";
    private ScrollSettings _scroll = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id
    {
        get => _id;
        set => SetField(ref _id, value?.Trim() ?? string.Empty);
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, string.IsNullOrWhiteSpace(value) ? "Новый профиль" : value.Trim());
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
    public bool IsGlobal { get; set; }

    [JsonIgnore]
    public double ProfileScrollMultiplier
    {
        get => Scroll.ScrollMultiplier;
        set
        {
            if (Math.Abs(Scroll.ScrollMultiplier - value) < 0.001)
            {
                return;
            }

            Scroll.ScrollMultiplier = value;
            Scroll.Validate();
            OnPropertyChanged();
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
        }
    }

    [JsonIgnore]
    public double ProfileSmoothness
    {
        get => Scroll.Smoothness;
        set
        {
            if (Math.Abs(Scroll.Smoothness - value) < 0.001)
            {
                return;
            }

            Scroll.Smoothness = value;
            Scroll.Validate();
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public double ProfileAcceleration
    {
        get => Scroll.Acceleration;
        set
        {
            if (Math.Abs(Scroll.Acceleration - value) < 0.001)
            {
                return;
            }

            Scroll.Acceleration = value;
            Scroll.Validate();
            OnPropertyChanged();
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
        OnPropertyChanged(nameof(ProfileScrollMultiplier));
        OnPropertyChanged(nameof(ProfileDurationMs));
        OnPropertyChanged(nameof(ProfileSmoothness));
        OnPropertyChanged(nameof(ProfileAcceleration));
        OnPropertyChanged(nameof(ProfileEasingType));
        OnPropertyChanged(nameof(ProfileEnableHorizontalScroll));
    }
}
