using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    public int OpacityPercent { get; set; } = 100;

    // v0.3.1 and older stored transparency percentage. Keep this nullable property only for migration.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TransparencyPercent { get; set; }

    public bool TopMost { get; set; } = true;
    public bool MouseGesturesEnabled { get; set; } = true;
    public int GestureDefaultsVersion { get; set; } = 0;
    public Dictionary<string, string> MouseGestures { get; set; } = GestureActions.CreateDefaultMap();

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

    public Dictionary<string, string> GetNormalizedGestureActions()
    {
        NormalizeGestures();
        return new Dictionary<string, string>(MouseGestures, StringComparer.OrdinalIgnoreCase);
    }

    public string GetGestureAction(string pattern)
    {
        var actions = GetNormalizedGestureActions();
        return actions.TryGetValue(pattern, out var action) ? GestureActions.NormalizeAction(action) : GestureActions.None;
    }

    public void SetGestureActions(Dictionary<string, string> actions)
    {
        MouseGestures = actions ?? GestureActions.CreateDefaultMap();
        GestureDefaultsVersion = GestureActions.DefaultMapVersion;
        NormalizeGestures();
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

        if (TransparencyPercent.HasValue)
        {
            OpacityPercent = AppOptions.OpacityFromTransparency(TransparencyPercent.Value);
            TransparencyPercent = null;
        }

        OpacityPercent = AppOptions.NormalizeOpacity(OpacityPercent);
        NormalizeGestures();
    }

    private void NormalizeGestures()
    {
        var defaults = GestureActions.CreateDefaultMap();
        var current = MouseGestures ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (GestureDefaultsVersion < GestureActions.DefaultMapVersion && GestureActions.LooksLikeLegacyDefaultMap(current))
        {
            current = defaults;
        }

        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pattern in GesturePatterns.All)
        {
            var value = current.TryGetValue(pattern.Key, out var action)
                ? GestureActions.NormalizeAction(action)
                : defaults[pattern.Key];
            normalized[pattern.Key] = value;
        }

        MouseGestures = normalized;
        GestureDefaultsVersion = GestureActions.DefaultMapVersion;
    }
}
