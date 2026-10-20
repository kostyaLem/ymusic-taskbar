using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using YMusicTaskbar;
using Forms = System.Windows.Forms;

internal enum IntegrationMode { Full, MenuOnly, PlacementOnly }

internal sealed class IntegrationFixture : IDisposable
{
    private readonly string executable, directory, settingsFile;
    private readonly string? savedSettings;
    private Process? player;
    private readonly IntegrationMode mode;
    public int Count { get; private set; }
    public int ProcessId => player!.Id;
    public IntegrationFixture(string executable, string directory, IntegrationMode mode = IntegrationMode.Full)
    {
        this.executable = Path.GetFullPath(executable); this.directory = Path.GetFullPath(directory); this.mode = mode;
        settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YMusicTaskbar", "settings.json");
        if (File.Exists(settingsFile)) savedSettings = File.ReadAllText(settingsFile);
    }
    public void Check(bool success, string label)
    { if (!success) throw new Exception(label); Console.WriteLine("PASS " + label); Count++; }
    public void Dispose()
    {
        if (player is not null)
        {
            File.WriteAllText(Path.Combine(directory, "stop.txt"), "stop");
            if (!player.WaitForExit(3000)) player.Kill();
            player.Dispose();
        }
        if (savedSettings is not null) File.WriteAllText(settingsFile, savedSettings);
        else if (File.Exists(settingsFile)) File.Delete(settingsFile);
    }
    public async Task<JsonDocument> InspectAsync(string name, Func<JsonElement, bool>? ready = null)
    {
        await Task.Delay(1800);
        string path = Path.Combine(directory, "live.json");
        string json;
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            if (player is null || player.HasExited) throw new Exception(name + ": player exited before diagnostics were ready");
            if (File.Exists(path))
            {
                json = await File.ReadAllTextAsync(path);
                using var snapshot = JsonDocument.Parse(json);
                if ((name != "baseline" || snapshot.RootElement.GetProperty("Monitors").GetArrayLength() > 0) && (ready is null || ready(snapshot.RootElement))) break;
            }
            if (DateTime.UtcNow >= deadline) throw new TimeoutException(name + ": desktop state did not become ready");
            await Task.Delay(250);
        }
        Check(!player.HasExited, name + ": live player is running");
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), json);
        return JsonDocument.Parse(json);
    }
    public async Task RunAsync()
    {
        Directory.CreateDirectory(directory);
        string stop = Path.Combine(directory, "stop.txt"); if (File.Exists(stop)) File.Delete(stop);
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add("--test-host"); start.ArgumentList.Add("--output"); start.ArgumentList.Add(directory);
        start.ArgumentList.Add("--full");
        player = Process.Start(start) ?? throw new Exception("Cannot start player");
        var screens = Forms.Screen.AllScreens.OrderByDescending(s => s.Primary).ToArray();
        Check(screens.Length >= 1, "Interactive monitor available");
        using (var baseline = await InspectAsync("baseline"))
        {
            var root = baseline.RootElement;
            var system = root.GetProperty("OperatingSystem");
            Check(system.GetProperty("BuildNumber").GetInt32() == Environment.OSVersion.Version.Build, "Diagnostics contain actual OS build");
            Check(system.GetProperty("Revision").GetInt32() >= 0 && system.GetProperty("Type").GetString() is { Length: > 0 }, "Diagnostics contain OS type and update revision");
            Check(root.GetProperty("MusicRunning").GetBoolean(), "Supported music process detected");
            Check(CompatibilityIds.IsMusicSession(root.GetProperty("Media").GetProperty("Source").GetString()), "Correct media session selected");
            Check(root.GetProperty("Placement").ValueKind == JsonValueKind.Object, "Free taskbar area found");
            Check(root.GetProperty("Visible").GetBoolean(), "Panel is actually visible");
            if (mode == IntegrationMode.MenuOnly) { await MenuIntegrationScenarios.RunAsync(this, root); return; }
            if (mode != IntegrationMode.PlacementOnly)
            {
                Check(root.GetProperty("Audio").GetProperty("Available").GetBoolean(), "App-specific audio session readable");
                await AudioNotificationScenarios.RunAsync(this, root);
                await PlayerControlScenarios.RunAsync(this, root);
                using (var audio = new AudioService())
                {
                    audio.Refresh();
                    double original = audio.Level;
                    try
                    {
                        double probe = original > 1 ? original - 1 : original + 1;
                        audio.SetLevel(probe); audio.Refresh();
                        Check(Math.Abs(audio.Level - probe) < 0.05, "Native app-volume change reads back correctly");
                    }
                    finally { audio.SetLevel(original); }
                    audio.Refresh();
                    Check(Math.Abs(audio.Level - original) < 0.05, "Original app volume restored");
                }
            }
            string primary = root.GetProperty("Placement").GetProperty("Monitor").GetString()!;
            var selectedScreen = screens.First(s => s.DeviceName == primary);
            Window? first = null, second = null;
            try
            {
                first = TestWindows.Cover(selectedScreen, false);
                await Task.Delay(200);
                using (var windowed = await InspectAsync("windowed"))
                    Check(windowed.RootElement.GetProperty("Placement").GetProperty("Monitor").GetString() == primary, "Normal window does not displace player");
                first.Close(); first = TestWindows.Cover(selectedScreen, true);
                await Task.Delay(200);
                Native.GetWindowRect(new WindowInteropHelper(first).Handle, out var coverBounds);
                Check(coverBounds.Left <= selectedScreen.Bounds.Left && coverBounds.Top <= selectedScreen.Bounds.Top && coverBounds.Right >= selectedScreen.Bounds.Right && coverBounds.Bottom >= selectedScreen.Bounds.Bottom,
                    $"Fullscreen test window covers monitor: {coverBounds.Left},{coverBounds.Top},{coverBounds.Right},{coverBounds.Bottom}");
                using (var one = await InspectAsync("one-fullscreen", frame => frame.GetProperty("Monitors").EnumerateArray().Any(m => m.GetProperty("Monitor").GetString() == primary && m.GetProperty("Fullscreen").GetBoolean())))
                {
                    var placement = one.RootElement.GetProperty("Placement");
                    bool otherAvailable = root.GetProperty("Monitors").EnumerateArray().Any(m =>
                        m.GetProperty("Monitor").GetString() != primary && !m.GetProperty("Fullscreen").GetBoolean() && m.GetProperty("Visible").GetBoolean());
                    if (otherAvailable)
                    {
                        Check(placement.ValueKind == JsonValueKind.Object && placement.GetProperty("Monitor").GetString() != primary, "Fullscreen moves placement to second screen");
                        Check(one.RootElement.GetProperty("Visible").GetBoolean(), "Panel stays visible on second monitor");
                    }
                    else
                    {
                        Check(placement.ValueKind == JsonValueKind.Null, "Covering last available screen hides placement");
                        Check(!one.RootElement.GetProperty("Visible").GetBoolean(), "Panel hides when no other screen is available");
                    }
                }
                if (screens.Length > 1)
                {
                    second = TestWindows.Cover(screens.First(s => s.DeviceName != primary), true);
                    await Task.Delay(200);
                    using (var both = await InspectAsync("both-fullscreen", frame => frame.GetProperty("Monitors").EnumerateArray().All(m => m.GetProperty("Fullscreen").GetBoolean())))
                    {
                        Check(both.RootElement.GetProperty("Placement").ValueKind == JsonValueKind.Null, "Both fullscreen screens hide player");
                        Check(!both.RootElement.GetProperty("Visible").GetBoolean(), "Panel window is actually hidden");
                    }
                }
                else Console.WriteLine("INFO Cross-monitor transfer requires a second display; single-screen hiding was verified.");
            }
            finally { second?.Close(); first?.Close(); }
            await Task.Delay(200);
            using var restored = await InspectAsync("restored", frame => frame.GetProperty("Visible").GetBoolean() && frame.GetProperty("Monitors").EnumerateArray().Any(m => m.GetProperty("Monitor").GetString() == primary && !m.GetProperty("Fullscreen").GetBoolean()));
            Check(restored.RootElement.GetProperty("Placement").GetProperty("Monitor").GetString() == primary, "Return to preferred screen after fullscreen");
            Check(restored.RootElement.GetProperty("Visible").GetBoolean(), "Panel reappears after fullscreen");
        }
        var quit = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true };
        quit.ArgumentList.Add("--quit");
        using var request = Process.Start(quit)!;
        await request.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        await player.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Check(request.ExitCode == 0 && player.ExitCode == 0, "Installer shutdown command exits observer gracefully");
    }
}
