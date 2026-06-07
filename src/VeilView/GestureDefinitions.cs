using System;
using System.Collections.Generic;
using System.Linq;

namespace VeilView;

internal static class GesturePatterns
{
    public const string Left = "Left";
    public const string Right = "Right";
    public const string Vertical = "Vertical";
    public const string Horizontal = "Horizontal";
    public const string CornerTopLeft = "CornerTopLeft";
    public const string CornerTopRight = "CornerTopRight";
    public const string CornerBottomRight = "CornerBottomRight";
    public const string CornerBottomLeft = "CornerBottomLeft";

    public static readonly GesturePatternDefinition[] All =
    {
        new(Left, "←", "왼쪽으로 드래그"),
        new(Right, "→", "오른쪽으로 드래그"),
        new(Vertical, "↕", "위아래 또는 아래위로 드래그"),
        new(Horizontal, "↔", "좌우 또는 우좌로 드래그"),
        new(CornerTopLeft, "┌", "왼쪽 위 모서리 형태"),
        new(CornerTopRight, "┐", "오른쪽 위 모서리 형태"),
        new(CornerBottomRight, "┘", "오른쪽 아래 모서리 형태"),
        new(CornerBottomLeft, "└", "왼쪽 아래 모서리 형태")
    };

    public static string DisplayName(string pattern)
        => All.FirstOrDefault(item => item.Key.Equals(pattern, StringComparison.OrdinalIgnoreCase))?.Display ?? pattern;
}

internal static class GestureActions
{
    public const int DefaultMapVersion = 2;

    public const string None = "None";
    public const string PreviousTab = "PreviousTab";
    public const string NextTab = "NextTab";
    public const string CloseTab = "CloseTab";
    public const string Back = "Back";
    public const string Forward = "Forward";
    public const string Reload = "Reload";
    public const string ToggleInputMode = "ToggleInputMode";

    public static readonly GestureActionDefinition[] All =
    {
        new(None, "동작 없음"),
        new(PreviousTab, "왼쪽 탭 이동"),
        new(NextTab, "오른쪽 탭 이동"),
        new(CloseTab, "탭 닫기"),
        new(Back, "이전 페이지"),
        new(Forward, "다음 페이지"),
        new(Reload, "새로고침"),
        new(ToggleInputMode, "직접 입력/작업창 복귀")
    };

    public static Dictionary<string, string> CreateDefaultMap() => new(StringComparer.OrdinalIgnoreCase)
    {
        [GesturePatterns.Left] = Back,
        [GesturePatterns.Right] = Forward,
        [GesturePatterns.Vertical] = Reload,
        [GesturePatterns.Horizontal] = ToggleInputMode,
        [GesturePatterns.CornerTopLeft] = NextTab,
        [GesturePatterns.CornerTopRight] = PreviousTab,
        [GesturePatterns.CornerBottomRight] = CloseTab,
        [GesturePatterns.CornerBottomLeft] = CloseTab
    };

    public static bool LooksLikeLegacyDefaultMap(Dictionary<string, string>? map)
    {
        if (map is null || map.Count == 0) return true;

        return Is(map, GesturePatterns.Left, Back)
               && Is(map, GesturePatterns.Right, Forward)
               && Is(map, GesturePatterns.Vertical, Reload)
               && Is(map, GesturePatterns.CornerTopLeft, PreviousTab)
               && Is(map, GesturePatterns.CornerTopRight, NextTab)
               && Is(map, GesturePatterns.CornerBottomRight, CloseTab)
               && Is(map, GesturePatterns.CornerBottomLeft, None);
    }

    public static string NormalizeAction(string? action)
    {
        if (string.IsNullOrWhiteSpace(action)) return None;
        return All.Any(item => item.Key.Equals(action, StringComparison.OrdinalIgnoreCase)) ? action : None;
    }

    public static string DisplayName(string action)
        => All.FirstOrDefault(item => item.Key.Equals(action, StringComparison.OrdinalIgnoreCase))?.Display ?? "동작 없음";

    private static bool Is(Dictionary<string, string> map, string key, string expected)
        => map.TryGetValue(key, out var value) && value.Equals(expected, StringComparison.OrdinalIgnoreCase);
}

internal sealed record GesturePatternDefinition(string Key, string Display, string Description);

internal sealed record GestureActionDefinition(string Key, string Display);
