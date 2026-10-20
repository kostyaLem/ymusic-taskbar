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

internal static class PlayerControlScenarios
{
    public static async Task RunAsync(IntegrationFixture test, JsonElement snapshot)
    {
        var hwnd = new IntPtr(snapshot.GetProperty("Ui").GetProperty("Handle").GetInt64());
        var element = AutomationElement.FromHandle(hwnd);
        AutomationElement Find(string id) => element.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, id)) ?? throw new Exception("Control not found: " + id);
        var volume = (RangeValuePattern)Find("Volume").GetCurrentPattern(RangeValuePattern.Pattern);
        var muteElement = element.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "Выключить / включить звук музыкального приложения"));
        var mute = (InvokePattern)muteElement.GetCurrentPattern(InvokePattern.Pattern);
        using (var audio = new AudioService())
        {
            audio.Refresh(); double original = audio.Level; bool wasMuted = audio.Muted;
            try
            {
                double target = original > 2 ? original - 2 : original + 2;
                await Task.Run(() => volume.SetValue(target)); await Task.Delay(300); audio.Refresh();
                test.Check(Math.Abs(audio.Level - target) < 0.05, "UI volume control changes actual application audio");
                await Task.Run(mute.Invoke); await Task.Delay(300); audio.Refresh();
                test.Check(audio.Muted && Math.Abs(audio.Level - target) < 0.05, "Speaker click mutes without losing level");
                await Task.Run(mute.Invoke); await Task.Delay(300); audio.Refresh();
                test.Check(!audio.Muted && Math.Abs(audio.Level - target) < 0.05, "Second speaker click restores sound and level");
                await Task.Run(muteElement.SetFocus);
                using var focused = await test.InspectAsync("button-focused");
                test.Check(focused.RootElement.GetProperty("Ui").GetProperty("IdleHighlightedButtons").GetInt32() == 0, "Keyboard focus never leaves a button highlighted");
            }
            finally { audio.SetLevel(original); audio.SetMuted(wasMuted); }
        }
        var seek = (RangeValuePattern)Find("Position").GetCurrentPattern(RangeValuePattern.Pattern);
        var media = snapshot.GetProperty("Media");
        double originalPosition = media.GetProperty("Position").GetDouble(), duration = media.GetProperty("Duration").GetDouble();
        bool playing = media.GetProperty("Playing").GetBoolean();
        double targetPosition = Math.Clamp(originalPosition + (originalPosition < duration / 2 ? 6 : -6), 1, duration - 2);
        var clock = Stopwatch.StartNew();
        try
        {
            await Task.Run(() => seek.SetValue(targetPosition));
            using var result = await test.InspectAsync("seek-command");
            double actual = result.RootElement.GetProperty("Media").GetProperty("Position").GetDouble();
            test.Check(Math.Abs(actual - targetPosition - (playing ? clock.Elapsed.TotalSeconds : 0)) < 3, "UI seek changes real media position");
        }
        finally { await Task.Run(() => seek.SetValue(Math.Clamp(originalPosition + (playing ? clock.Elapsed.TotalSeconds : 0), 0, duration - 1))); }
        await SeekBoundaryScenarios.RunAsync(test, seek);
        test.Check(snapshot.GetProperty("Ui").GetProperty("Transparent").GetBoolean(), "Transparent window enabled");
        using var stable = await test.InspectAsync("stable-placement");
        test.Check(stable.RootElement.GetProperty("Ui").GetProperty("PlacementChanges").GetInt32() == snapshot.GetProperty("Ui").GetProperty("PlacementChanges").GetInt32(), "Periodic refresh does not reposition window");
        var settings = new SettingsDriver(test.ProcessId);
        Task Mode(bool compact) => settings.Mode(snapshot.GetProperty("Placement").GetProperty("Monitor").GetString()!, compact);
        await Mode(true);
        using (var compact = await test.InspectAsync("compact-mode"))
        {
            test.Check(compact.RootElement.GetProperty("Ui").GetProperty("Compact").GetBoolean(), "Compact mode selectable in settings");
            test.Check(!compact.RootElement.GetProperty("Ui").GetProperty("SeekVisible").GetBoolean(), "Compact mode hides seek row");
            test.Check(compact.RootElement.GetProperty("Ui").GetProperty("Width").GetDouble() < 430, "Compact mode removes horizontal reserved space");
            test.Check(compact.RootElement.GetProperty("Ui").GetProperty("Height").GetDouble() < 34, "Compact mode removes seek row height");
        }
        await Mode(false);
        using var full = await test.InspectAsync("full-mode");
        test.Check(full.RootElement.GetProperty("Ui").GetProperty("SeekVisible").GetBoolean(), "Full mode restores seek row");
        await Task.Run(() =>
        {
            var more = element.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "Настройки и выход"));
            ((InvokePattern)more.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
        });
        await Task.Delay(300);
        var place = full.RootElement.GetProperty("Placement");
        Native.SetForegroundWindow(Native.TaskbarAt(place.GetProperty("X").GetInt32(), place.GetProperty("Y").GetInt32()));
        using var activated = await test.InspectAsync("taskbar-activated");
        test.Check(activated.RootElement.GetProperty("Visible").GetBoolean(), "Panel stays visible after menu and taskbar activation");
        test.Check(activated.RootElement.GetProperty("Ui").GetProperty("PlacementChanges").GetInt32() == full.RootElement.GetProperty("Ui").GetProperty("PlacementChanges").GetInt32(), "Menu dismissal does not reposition panel");
        var volumeRect = Find("Volume").Current.BoundingRectangle;
        var hit = Native.WindowFromPoint(new Native.Point { X = (int)(volumeRect.Left + volumeRect.Width / 2), Y = (int)(volumeRect.Top + 2) });
        test.Check(Native.GetAncestor(hit, 2) == hwnd, "Transparent control hit area stays above active taskbar");
    }
}
