using System.Collections.Generic;
namespace YMusicTaskbar;

public enum ThemeMode { System, Light, Dark }
public enum PlayerSide { Left, Right }
public sealed class MonitorProfile
{
    public bool Compact { get; set; }
    public PlayerSide Side { get; set; }
    public bool Enabled { get; set; } = true;
}
public sealed class Settings
{
    public string? PreferredMonitor { get; set; }
    public bool Compact { get; set; }
    public ThemeMode Theme { get; set; }
    public Dictionary<string, MonitorProfile> Monitors { get; set; } = new();
    public MonitorProfile ProfileFor(string monitor) => Monitors.TryGetValue(monitor, out var profile)
        ? profile : new MonitorProfile { Compact = Compact };
}