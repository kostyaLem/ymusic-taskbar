using Microsoft.Win32;
using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;
namespace YMusicTaskbar;

public static class ThemeService
{
    private static readonly ConditionalWeakTable<ResourceDictionary, System.Collections.Generic.Dictionary<string, ThemeColor>> Palettes = new();
    private sealed class ThemeColor : DependencyObject
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(Color), typeof(ThemeColor));
        public Color Value { get => (Color)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    }
    public static bool IsLight(ThemeMode mode)
    {
        if (mode != ThemeMode.System) return mode == ThemeMode.Light;
        try { return Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme", 0) is int value && value != 0; }
        catch { return false; }
    }
    public static void Apply(ResourceDictionary resources, ThemeMode mode)
    {
        // Color bindings keep brushes mutable when WPF seals resource dictionaries.
        // Only color tokens change afterwards, preserving resource and style identities.
        if (!Palettes.TryGetValue(resources, out var palette))
        {
            palette = new();
            foreach (string key in new[] { "PlayerText", "PlayerSecondary", "PlayerAccent", "PlayerSurface", "PlayerBorder", "PlayerHover", "PlayerPressed", "PlayerRail" })
            {
                var token = new ThemeColor { Value = ((SolidColorBrush)resources[key]).Color };
                var brush = new SolidColorBrush();
                BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, new Binding(nameof(ThemeColor.Value)) { Source = token });
                palette[key] = token; resources[key] = brush;
            }
            Palettes.Add(resources, palette);
        }
        bool light = IsLight(mode);
        string[] keys = { "PlayerText", "PlayerSecondary", "PlayerAccent", "PlayerSurface", "PlayerBorder", "PlayerHover", "PlayerPressed", "PlayerRail" };
        string[] colors = light
            ? new[] { "#202027", "#50505C", "#805A00", "#F6F6F9", "#CCCCD4", "#E3E3E9", "#D3D3DD", "#92929E" }
            : new[] { "#F4F4F5", "#BCBCC5", "#FFD641", "#202026", "#3A3A44", "#34343E", "#454552", "#666670" };
        for (int i = 0; i < keys.Length; i++)
        {
            var color = (Color)ColorConverter.ConvertFromString(colors[i]);
            if (palette[keys[i]].Value != color) palette[keys[i]].Value = color;
        }
    }
}