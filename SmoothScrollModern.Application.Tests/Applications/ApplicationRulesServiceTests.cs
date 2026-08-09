using SmoothScrollModern.Applications;
using SmoothScrollModern.Settings;

namespace SmoothScrollModern.Application.Tests.Applications;

public sealed class ApplicationRulesServiceTests
{
    [Fact]
    public void AddApplicationPath_KeepsDistinctRulesForDifferentCopiesOfTheSameExecutable()
    {
        var settings = new AppSettings();
        var service = new ApplicationRulesService();

        var first = service.AddApplicationPath(settings, @"C:\Release\reader.exe", "Reader", disableSmoothScroll: true);
        var second = service.AddApplicationPath(settings, @"D:\Portable\reader.exe", "Reader Portable", disableSmoothScroll: true);

        Assert.NotSame(first, second);
        Assert.Equal(2, settings.ApplicationRules.Count);
    }

    [Fact]
    public void Matches_PathRuleDoesNotFallbackToProcessNameWhenPathIsUnavailable()
    {
        var rule = new ApplicationRule
        {
            ProcessName = "reader.exe",
            ExecutablePath = @"C:\Release\reader.exe"
        };
        var application = new ApplicationInfo(
            (IntPtr)1,
            1,
            "reader.exe",
            string.Empty,
            "Reader",
            "Document",
            false);

        Assert.False(ApplicationRulesService.Matches(rule, application));
    }
}
