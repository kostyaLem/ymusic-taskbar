using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using YMusicTaskbar;

internal static class MenuIntegrationScenarios
{
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, uint message, IntPtr key, IntPtr parameter);

    public static async Task RunAsync(IntegrationFixture test, JsonElement snapshot)
    {
        var settings = new SettingsDriver(test.ProcessId);
        var hwnd = new IntPtr(snapshot.GetProperty("Ui").GetProperty("Handle").GetInt64());
        async Task Open() => await settings.Invoke("Настройки и выход", ControlType.Button);
        async Task Key(int key)
        {
            var window = GetForegroundWindow(); Native.GetWindowThreadProcessId(window, out uint owner);
            test.Check(owner == test.ProcessId, "Menu owns keyboard focus");
            MenuKeyboard.Press(key); await Task.Delay(300);
        }
        async Task StableDuring(Func<Task> action, string label)
        {
            Native.GetWindowRect(hwnd, out var expected);
            using var stop = new CancellationTokenSource();
            var probe = Task.Run(async () =>
            {
                bool stable = true;
                while (!stop.IsCancellationRequested)
                {
                    stable &= Native.GetWindowRect(hwnd, out var current) && current.Width == expected.Width && current.Height == expected.Height;
                    await Task.Delay(20);
                }
                return stable;
            });
            try { await action(); await Task.Delay(1300); }
            finally { stop.Cancel(); }
            test.Check(await probe, label);
        }
        await Open();
        test.Check(settings.Find("Настройки…", ControlType.MenuItem) is not null, "Custom menu remains accessible");
        test.Check(settings.Find("Вид плеера", ControlType.MenuItem) is null && settings.Find("Запускать вместе с Windows", ControlType.MenuItem) is null
            && settings.Find("Сохранить диагностику", ControlType.MenuItem) is null, "Configuration and diagnostics are absent from quick menu");
        await Key(0x1B);
        test.Check(settings.Find("Настройки…", ControlType.MenuItem) is null, "Escape closes quick menu");
        await StableDuring(async () =>
        {
            await Open(); await Key(0x24); await Key(0x28); await Key(0x0D);
            test.Check(settings.Find("Тема", ControlType.ComboBox) is not null, "Keyboard opens separate settings window");
            await settings.Invoke("Отмена", ControlType.Button);
        }, "Opening and closing settings never changes overlay dimensions");
        foreach (string theme in new[] { "Тёмная", "Светлая" })
        {
            await StableDuring(async () =>
            {
                await settings.Open(); await settings.Select("Тема", theme); await settings.Save();
                string expectedColor = theme == "Тёмная" ? "#FFF4F4F5" : "#FF202027";
                using var applied = await test.InspectAsync("applied-theme-" + expectedColor[1..], s => s.GetProperty("Ui").GetProperty("TextColor").GetString() == expectedColor);
                test.Check(applied.RootElement.GetProperty("Ui").GetProperty("TextColor").GetString() == expectedColor, "Saved theme reaches live player: " + theme);
            }, "Applying theme preserves every sampled overlay size: " + theme);
        }
        using var full = await test.InspectAsync("theme-stable");
        await Open();
        var place = full.RootElement.GetProperty("Placement");
        Native.SetForegroundWindow(Native.TaskbarAt(place.GetProperty("X").GetInt32(), place.GetProperty("Y").GetInt32()));
        await Task.Delay(400);
        test.Check(settings.Find("Настройки…", ControlType.MenuItem) is null, "Outside activation dismisses menu");
        using var stable = await test.InspectAsync("menu-dismissed");
        test.Check(stable.RootElement.GetProperty("Visible").GetBoolean() && stable.RootElement.GetProperty("Ui").GetProperty("PlacementChanges").GetInt32() == full.RootElement.GetProperty("Ui").GetProperty("PlacementChanges").GetInt32(), "Menu dismissal leaves panel visible without repositioning");
        var trayWindows = new List<IntPtr>();
        Native.EnumWindows((handle, _) =>
        {
            Native.GetWindowThreadProcessId(handle, out uint owner);
            if (owner == test.ProcessId && Native.ClassName(handle).StartsWith("WindowsForms", StringComparison.Ordinal)) trayWindows.Add(handle);
            return true;
        }, IntPtr.Zero);
        test.Check(trayWindows.Count > 0, "Native tray callback window exists");
        foreach (int mouseUp in new[] { 0x202, 0x205 })
        {
            foreach (var handle in trayWindows) PostMessage(handle, 0x800, IntPtr.Zero, new IntPtr(mouseUp));
            await Task.Delay(400);
            test.Check(settings.Find("Настройки…", ControlType.MenuItem) is not null, mouseUp == 0x202 ? "Left tray callback opens custom menu" : "Right tray callback opens custom menu");
            await Key(0x1B);
        }
    }
}