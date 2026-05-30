using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace VeilView;

internal sealed class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string LastUrl { get; set; } = "https://www.youtube.com/";
    public string[] LastTabs { get; set; } = Array.Empty<string>();
    public int ActiveTabIndex { get; set; } = 0;
    public int X { get; set; } = 80;
    public int Y { get; set; } = 80;
    public int Width { get; set; } = 960;
    public int Height { get; set; } = 540;
    public int TransparencyPercent { get; set; } = 0;
    public bool TopMost { get; set; } = true;

    public static string AppDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VeilView");

    public static string SettingsPath => Path.Combine(AppDataDirectory, "settings.json");

    public static string WebViewUserDataFolder => Path.Combine(AppDataDirectory, "WebView2UserData");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            loaded.Normalize();
            return loaded;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Normalize();
        Directory.CreateDirectory(AppDataDirectory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
    }

    private void Normalize()
    {
        LastUrl = string.IsNullOrWhiteSpace(LastUrl) ? "https://www.youtube.com/" : LastUrl.Trim();
        LastTabs = (LastTabs ?? Array.Empty<string>())
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Take(20)
            .ToArray();
        ActiveTabIndex = Math.Clamp(ActiveTabIndex, 0, Math.Max(0, LastTabs.Length - 1));
        Width = Math.Max(520, Width);
        Height = Math.Max(320, Height);
        TransparencyPercent = AppOptions.NormalizeTransparency(TransparencyPercent);
    }
}
