namespace SmoothScrollModern.Settings;

public interface ISettingsService
{
    string SettingsPath { get; }

    AppSettings Load();

    void Save(AppSettings settings);

    void Export(AppSettings settings, string path);

    AppSettings Import(string path);
}
