using SmoothScrollModern.Applications;
using SmoothScrollModern.Scroll;
using SmoothScrollModern.Settings;

namespace SmoothScrollModern.Application.Tests.Scroll;

public sealed class ScrollDecisionServiceTests
{
    [Fact]
    public void Decide_PathRuleDoesNotApplyToAnotherCopyOfTheSameProcess()
    {
        var resolver = new FakeWindowIdentityResolver(new WindowIdentity(
            (IntPtr)42,
            "browser.exe",
            @"D:\Portable\browser.exe",
            false));
        var service = CreateService(resolver, new AppSettings
        {
            ApplicationRules =
            [
                new ApplicationRule
                {
                    ProcessName = "browser.exe",
                    ExecutablePath = @"C:\Program Files\Browser\browser.exe",
                    IsSmoothScrollDisabled = true
                }
            ]
        });

        var decision = service.Decide((IntPtr)42);

        Assert.True(decision.ShouldSmooth);
    }

    [Fact]
    public void Decide_SelectedOnlyRequiresAnEnabledMatchingRule()
    {
        var resolver = new FakeWindowIdentityResolver(new WindowIdentity(
            (IntPtr)42,
            "reader.exe",
            string.Empty,
            false));
        var service = CreateService(resolver, new AppSettings
        {
            ApplicationListMode = ApplicationListMode.SelectedOnly,
            ApplicationRules =
            [
                new ApplicationRule
                {
                    ProcessName = "reader.exe",
                    IsRuleEnabled = false
                }
            ]
        });

        var decision = service.Decide((IntPtr)42);

        Assert.False(decision.ShouldSmooth);
    }

    [Fact]
    public void Decide_UsesProfileAndDeliveryModeFromAnEnabledRule()
    {
        var profile = new ScrollProfile
        {
            Id = "reader",
            Scroll = new ScrollSettings { DistanceMultiplier = 1.75 }
        };
        var resolver = new FakeWindowIdentityResolver(new WindowIdentity(
            (IntPtr)42,
            "reader.exe",
            string.Empty,
            false));
        var service = CreateService(resolver, new AppSettings
        {
            ScrollProfiles = [profile],
            ApplicationRules =
            [
                new ApplicationRule
                {
                    ProcessName = "reader.exe",
                    ScrollProfileId = profile.Id,
                    IsSmoothScrollDisabled = false,
                    DeliveryMode = ScrollDeliveryMode.WheelStep
                }
            ]
        });

        var decision = service.Decide((IntPtr)42);

        Assert.True(decision.ShouldSmooth);
        Assert.NotNull(decision.Settings);
        Assert.Equal(1.75, decision.Settings.DistanceMultiplier);
        Assert.Equal(ScrollDeliveryMode.WheelStep, decision.DeliveryMode);
    }

    [Fact]
    public void Decide_BypassesFullscreenWindowWhenAutomaticExclusionEnabled()
    {
        var resolver = new FakeWindowIdentityResolver(new WindowIdentity(
            (IntPtr)42,
            "game.exe",
            string.Empty,
            true));
        var service = CreateService(resolver, new AppSettings { AutoDetectExcludedApps = true });

        var decision = service.Decide((IntPtr)42);

        Assert.False(decision.ShouldSmooth);
    }

    [Fact]
    public void Decide_BypassesActivePause()
    {
        var resolver = new FakeWindowIdentityResolver(new WindowIdentity(
            (IntPtr)42,
            "reader.exe",
            string.Empty,
            false));
        var factory = new ScrollConfigurationSnapshotFactory();
        var configuration = factory.Create(new AppSettings(), DateTimeOffset.UtcNow.AddMinutes(1));
        var service = new ScrollDecisionService(new FakeConfigurationProvider(configuration), resolver);

        var decision = service.Decide((IntPtr)42);

        Assert.False(decision.ShouldSmooth);
    }

    private static ScrollDecisionService CreateService(IWindowIdentityResolver resolver, AppSettings settings)
    {
        var configuration = new ScrollConfigurationSnapshotFactory().Create(settings, pausedUntilUtc: null);
        return new ScrollDecisionService(new FakeConfigurationProvider(configuration), resolver);
    }

    private sealed class FakeConfigurationProvider(ScrollConfigurationSnapshot configuration) : IScrollConfigurationProvider
    {
        public ScrollConfigurationSnapshot Current { get; } = configuration;
    }

    private sealed class FakeWindowIdentityResolver(WindowIdentity identity) : IWindowIdentityResolver
    {
        public WindowIdentity Resolve(IntPtr windowHandle) => identity;
    }
}
