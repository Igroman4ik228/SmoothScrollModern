using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using SmoothScrollModern.Core;
using SmoothScrollModern.Scroll;

namespace SmoothScrollModern.Settings;

public sealed class JsonSettingsService : ISettingsService
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonSettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var directory = Path.Combine(appData, Constants.ApplicationName);
        Directory.CreateDirectory(directory);
        SettingsPath = Path.Combine(directory, Constants.SettingsFileName);
    }

    public string SettingsPath { get; }

    public AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return CreateDefaultSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _serializerOptions) ?? CreateDefaultSettings();
            settings.Validate();
            EnsureDefaultProfiles(settings);
            return settings;
        }
        catch (Exception)
        {
            BackupCorruptedSettings();
            return CreateDefaultSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        settings.Validate();
        var json = JsonSerializer.Serialize(settings, _serializerOptions);
        File.WriteAllText(SettingsPath, json);
    }

    public void Export(AppSettings settings, string path)
    {
        settings.Validate();
        var json = JsonSerializer.Serialize(settings, _serializerOptions);
        File.WriteAllText(path, json);
    }

    public AppSettings Import(string path)
    {
        var json = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<AppSettings>(json, _serializerOptions) ?? CreateDefaultSettings();
        settings.Validate();
        EnsureDefaultProfiles(settings);
        return settings;
    }

    private static AppSettings CreateDefaultSettings()
    {
        var settings = new AppSettings();
        EnsureDefaultProfiles(settings);
        return settings;
    }

    private static void EnsureDefaultProfiles(AppSettings settings)
    {
        if (settings.DefaultProfilesSeeded)
        {
            return;
        }

        AddDefaultProfile(settings, new ApplicationRule
        {
            ProcessName = "explorer.exe",
            DisplayName = "Проводник",
            IsSmoothScrollDisabled = false,
            IsRuleEnabled = true,
            IsUserRule = true,
            DeliveryMode = ScrollDeliveryMode.WheelStep
        });

        string[] disabledByDefault =
        [
            "steam.exe",
            "steamwebhelper.exe",
            "epicgameslauncher.exe",
            "battle.net.exe",
            "riotclientservices.exe",
            "valorant.exe",
            "cs2.exe",
            "dota2.exe",
            "minecraft.exe",
            "javaw.exe",
            "obs64.exe"
        ];

        foreach (var processName in disabledByDefault)
        {
            AddDefaultProfile(settings, new ApplicationRule
            {
                ProcessName = processName,
                DisplayName = processName,
                IsSmoothScrollDisabled = true,
                IsRuleEnabled = true,
                IsUserRule = true
            });
        }

        settings.DefaultProfilesSeeded = true;
    }

    private static void AddDefaultProfile(AppSettings settings, ApplicationRule profile)
    {
        profile.Validate();
        if (settings.ApplicationRules.Any(rule =>
                string.Equals(rule.ProcessName, profile.ProcessName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        settings.ApplicationRules.Add(profile);
    }

    private void BackupCorruptedSettings()
    {
        if (!File.Exists(SettingsPath))
        {
            return;
        }

        var backupPath = $"{SettingsPath}.{DateTime.Now:yyyyMMddHHmmss}.bak";
        File.Move(SettingsPath, backupPath, overwrite: true);
    }
}
