using SmoothScrollModern.Scroll;
using SmoothScrollModern.Settings;
using Windows.System;

namespace SmoothScrollModern.Application.Tests.Scroll;

public sealed class ScrollConfigurationSnapshotFactoryTests
{
    [Fact]
    public void Create_NormalizesValuesWithoutMutatingEditableSettings()
    {
        var settings = new AppSettings
        {
            Scroll = new ScrollSettings
            {
                DistanceMultiplier = double.NaN,
                Friction = 100,
                BypassSmoothingVirtualKeys = [VirtualKey.Control, VirtualKey.LeftControl, (VirtualKey)999]
            }
        };

        var snapshot = new ScrollConfigurationSnapshotFactory().Create(settings, pausedUntilUtc: null);

        Assert.Equal(ScrollSettings.DistanceMultiplierMin, snapshot.DefaultSettings.DistanceMultiplier);
        Assert.Equal(ScrollSettings.FrictionMax, snapshot.DefaultSettings.Friction);
        Assert.Equal([VirtualKey.LeftControl, VirtualKey.RightControl], snapshot.DefaultSettings.BypassSmoothingVirtualKeys.ToArray());
        Assert.True(double.IsNaN(settings.Scroll.DistanceMultiplier));
        Assert.Equal(100, settings.Scroll.Friction);
    }

    [Fact]
    public void Create_IndexesPathRulesSeparatelyFromProcessRules()
    {
        const string browserPath = @"C:\Browsers\browser.exe";
        var settings = new AppSettings
        {
            ApplicationRules =
            [
                new ApplicationRule
                {
                    ProcessName = "browser.exe",
                    ExecutablePath = browserPath,
                    IsSmoothScrollDisabled = true
                },
                new ApplicationRule
                {
                    ProcessName = "reader.exe",
                    IsSmoothScrollDisabled = false,
                    DeliveryMode = ScrollDeliveryMode.WheelStep
                }
            ]
        };

        var snapshot = new ScrollConfigurationSnapshotFactory().Create(settings, pausedUntilUtc: null);

        Assert.True(snapshot.RulesByExecutablePath.ContainsKey(browserPath));
        Assert.False(snapshot.RulesByProcess.ContainsKey("browser.exe"));
        Assert.True(snapshot.RulesByProcess.TryGetValue("reader.exe", out var readerRule));
        Assert.Equal(ScrollDeliveryMode.WheelStep, readerRule.DeliveryMode);
    }

    [Fact]
    public void Create_UsesReferencedProfileSettingsForRule()
    {
        var profile = new ScrollProfile
        {
            Id = "browser",
            Name = "Browser",
            Scroll = new ScrollSettings { DistanceMultiplier = 2.5 }
        };
        var settings = new AppSettings
        {
            ScrollProfiles = [profile],
            ApplicationRules =
            [
                new ApplicationRule
                {
                    ProcessName = "browser.exe",
                    ScrollProfileId = profile.Id
                }
            ]
        };

        var snapshot = new ScrollConfigurationSnapshotFactory().Create(settings, pausedUntilUtc: null);

        Assert.Equal(2.5, snapshot.RulesByProcess["browser.exe"].Settings.DistanceMultiplier);
    }
}
