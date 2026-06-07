using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VeilView;

internal sealed class AppOptions
{
    public string Url { get; init; } = "https://www.youtube.com/";
    public string[] StartTabs { get; init; } = Array.Empty<string>();
    public int X { get; init; } = 80;
    public int Y { get; init; } = 80;
    public int Width { get; init; } = 960;
    public int Height { get; init; } = 540;
    public int OpacityPercent { get; init; } = 100;
    public int TransparencyPercent { get; init; } = 0;
    public bool TopMost { get; init; } = true;
    public bool UrlWasSpecified { get; init; }

    public static AppOptions Parse(string[] args, AppSettings settings)
    {
        var mutable = new MutableOptions
        {
            Url = settings.LastUrl,
            X = settings.X,
            Y = settings.Y,
            Width = settings.Width,
            Height = settings.Height,
            OpacityPercent = NormalizeOpacity(settings.OpacityPercent),
            TopMost = settings.TopMost
        };

        for (var i = 0; i < args.Length; i++)
        {
            var key = args[i].Trim();
            var value = i + 1 < args.Length ? args[i + 1] : null;

            switch (key.ToLowerInvariant())
            {
                case "--url" when value is not null:
                    mutable.Url = value;
                    mutable.UrlWasSpecified = true;
                    i++;
                    break;
                case "--tab" when value is not null:
                    if (!mutable.UrlWasSpecified && mutable.StartTabs.Count == 0)
                    {
                        mutable.Url = value;
                        mutable.UrlWasSpecified = true;
                    }
                    else
                    {
                        mutable.StartTabs.Add(value);
                    }
                    i++;
                    break;
                case "--x" when TryInt(value, out var x):
                    mutable.X = x;
                    i++;
                    break;
                case "--y" when TryInt(value, out var y):
                    mutable.Y = y;
                    i++;
                    break;
                case "--width" when TryInt(value, out var width):
                    mutable.Width = Math.Max(520, width);
                    i++;
                    break;
                case "--height" when TryInt(value, out var height):
                    mutable.Height = Math.Max(320, height);
                    i++;
                    break;
                case "--transparency" when TryInt(value, out var transparency):
                    mutable.OpacityPercent = NormalizeOpacity(100 - NormalizeTransparency(transparency));
                    i++;
                    break;
                case "--transparent" when TryInt(value, out var transparent):
                    mutable.OpacityPercent = NormalizeOpacity(100 - NormalizeTransparency(transparent));
                    i++;
                    break;
                case "--opacity" when TryDouble(value, out var opacity):
                    mutable.OpacityPercent = NormalizeOpacity(opacity <= 1.0
                        ? (int)Math.Round(opacity * 100)
                        : (int)Math.Round(opacity));
                    i++;
                    break;
                case "--opacity-percent" when TryInt(value, out var opacityPercent):
                    mutable.OpacityPercent = NormalizeOpacity(opacityPercent);
                    i++;
                    break;
                case "--topmost" when TryBool(value, out var topMost):
                    mutable.TopMost = topMost;
                    i++;
                    break;
            }
        }

        var normalizedOpacity = NormalizeOpacity(mutable.OpacityPercent);
        return new AppOptions
        {
            Url = mutable.Url,
            StartTabs = mutable.StartTabs
                .Where(tab => !string.IsNullOrWhiteSpace(tab))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToArray(),
            X = mutable.X,
            Y = mutable.Y,
            Width = mutable.Width,
            Height = mutable.Height,
            OpacityPercent = normalizedOpacity,
            TransparencyPercent = 100 - normalizedOpacity,
            TopMost = mutable.TopMost,
            UrlWasSpecified = mutable.UrlWasSpecified
        };
    }

    private static bool TryInt(string? value, out int result)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    private static bool TryDouble(string? value, out double result)
        => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    private static bool TryBool(string? value, out bool result)
    {
        result = false;
        if (value is null) return false;
        if (bool.TryParse(value, out result)) return true;

        if (value is "1" || value.Equals("yes", StringComparison.OrdinalIgnoreCase) || value.Equals("on", StringComparison.OrdinalIgnoreCase))
        {
            result = true;
            return true;
        }

        if (value is "0" || value.Equals("no", StringComparison.OrdinalIgnoreCase) || value.Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            result = false;
            return true;
        }

        return false;
    }

    public static int NormalizeTransparency(int value) => Math.Clamp(value, 0, 70);

    public static int NormalizeOpacity(int value) => Math.Clamp(value, 30, 100);

    public static int OpacityFromTransparency(int transparencyPercent) => NormalizeOpacity(100 - NormalizeTransparency(transparencyPercent));

    public static int NormalizeOpacityPercent(int value) => NormalizeOpacity(value);

    public static int TransparencyFromOpacity(int opacityPercent) => 100 - NormalizeOpacity(opacityPercent);

    private sealed class MutableOptions
    {
        public string Url { get; set; } = "https://www.youtube.com/";
        public List<string> StartTabs { get; } = new();
        public int X { get; set; } = 80;
        public int Y { get; set; } = 80;
        public int Width { get; set; } = 960;
        public int Height { get; set; } = 540;
        public int OpacityPercent { get; set; } = 100;
        public bool TopMost { get; set; } = true;
        public bool UrlWasSpecified { get; set; }
    }
}
