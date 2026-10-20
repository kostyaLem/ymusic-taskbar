using System.Windows;
using System.Windows.Controls;
using YMusicTaskbar;

namespace YMusicTaskbar.AppTests;

internal static class SettingsScenarios
{
    public static void Run(Action<bool, string> check)
    {
        var original = new Settings { Theme = ThemeMode.Light, PreferredMonitor = "B", Compact = true };
        original.Monitors["A"] = new MonitorProfile { Side = PlayerSide.Right, Compact = true };
        original.Monitors["B"] = new MonitorProfile { Side = PlayerSide.Left, Compact = false };
        Settings? applied = null; bool startup = true; int calls = 0;
        SettingsWindow NewDialog(bool launch = true) => new(original, new[] { "A", "B" }, "A", launch,
            (settings, value) => { applied = settings; startup = value; calls++; });
        T Element<T>(SettingsWindow window, string name) where T : FrameworkElement => (T)window.FindName(name);
        void Click(SettingsWindow window, string name) => Element<Button>(window, name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        var dialog = NewDialog();
        check(calls == 0, "Opening settings does not persist or alter startup");
        Element<ComboBox>(dialog, "ThemeChoice").SelectedIndex = (int)ThemeMode.Dark;
        check(calls == 1 && applied is { Theme: ThemeMode.Dark }, "Theme change applies before closing settings");
        Element<CheckBox>(dialog, "StartupChoice").IsChecked = false;
        check(!startup, "Startup checkbox applies immediately");
        Element<ComboBox>(dialog, "PreferredChoice").SelectedIndex = 1;
        check(applied?.PreferredMonitor == "A", "Preferred display changes immediately");
        var tiles = Element<ListBox>(dialog, "MonitorSelector");
        int beforeSelection = calls;
        tiles.SelectedIndex = 0;
        check(calls == beforeSelection, "Selecting a monitor tile alone does not save settings");
        Element<ComboBox>(dialog, "SideChoice").SelectedIndex = 0;
        check(applied?.Monitors["A"].Side == PlayerSide.Left, "Per-monitor alignment applies immediately");
        var lastValid = applied;
        Element<CheckBox>(dialog, "EnabledChoice").IsChecked = false;
        tiles.SelectedIndex = 1;
        Element<CheckBox>(dialog, "EnabledChoice").IsChecked = false;
        check(applied?.Monitors["B"].Enabled == true && Element<TextBlock>(dialog, "ErrorText").Text.Length > 0,
            "Disabling the last display leaves the previous valid runtime settings intact");
        check(lastValid?.Monitors["A"].Enabled == true, "Later draft mutations never alter previously applied profile objects");
        Element<CheckBox>(dialog, "EnabledChoice").IsChecked = true;
        Click(dialog, "CancelButton");
        check(applied is { Theme: ThemeMode.Light, PreferredMonitor: "B", Compact: true } && startup
            && applied.Monitors["A"].Side == PlayerSide.Right && applied.Monitors["A"].Enabled,
            "Cancel restores all opening settings and startup after live edits");

        dialog = NewDialog();
        Click(dialog, "ResetButton");
        check(applied is { Theme: ThemeMode.System, PreferredMonitor: null, Compact: false } && !startup
            && applied.Monitors.Count == 2 && applied.Monitors.Values.All(p => p.Enabled && !p.Compact && p.Side == PlayerSide.Left),
            "Reset immediately applies default theme, displays, layout, alignment and startup");
        check(Element<ListBox>(dialog, "MonitorSelector").SelectedIndex == 1 && Element<ComboBox>(dialog, "LayoutChoice").SelectedIndex == 0,
            "Reset updates selected monitor controls without changing selected tile");
        dialog.Close();
        check(applied?.Theme == ThemeMode.Light && applied.PreferredMonitor == "B" && startup,
            "Closing settings without OK rolls back reset as well as ordinary edits");

        dialog = NewDialog();
        Element<ComboBox>(dialog, "ThemeChoice").SelectedIndex = (int)ThemeMode.Dark;
        int beforeOk = calls;
        Click(dialog, "OkButton");
        check(applied?.Theme == ThemeMode.Dark && calls == beforeOk, "OK retains live edits without reapplying or rolling back");
        check(original.Theme == ThemeMode.Light && original.Monitors["A"].Side == PlayerSide.Right,
            "Live editing and reset preserve caller-owned opening settings");

        int beforeUnchanged = calls;
        dialog = NewDialog();
        Click(dialog, "CancelButton");
        check(calls == beforeUnchanged, "Cancel without edits does not rewrite settings");

        bool failRestore = true, closed = false;
        dialog = new SettingsWindow(original, new[] { "A", "B" }, "A", true, (settings, value) =>
        {
            if (failRestore && settings.Theme == ThemeMode.Light) throw new System.IO.IOException("Restore failed");
            applied = settings; startup = value;
        });
        dialog.Closed += (_, _) => closed = true;
        Element<ComboBox>(dialog, "ThemeChoice").SelectedIndex = (int)ThemeMode.Dark;
        Click(dialog, "CancelButton");
        check(!closed && Element<TextBlock>(dialog, "ErrorText").Text.Contains("Restore failed"),
            "Rollback failure leaves settings open with a visible error");
        failRestore = false;
        Click(dialog, "CancelButton");
        check(closed && applied?.Theme == ThemeMode.Light && startup, "Cancel retries a failed rollback before closing");

        bool failOnce = true, registered = false;
        dialog = new SettingsWindow(original, new[] { "A", "B" }, "A", false, (_, value) =>
        {
            registered = value;
            if (failOnce) { failOnce = false; throw new System.IO.IOException("Partial apply failed"); }
        });
        Element<CheckBox>(dialog, "StartupChoice").IsChecked = true;
        Element<CheckBox>(dialog, "StartupChoice").IsChecked = false;
        check(!registered && Element<TextBlock>(dialog, "ErrorText").Text.Length == 0,
            "Returning to a prior value after partial apply failure retries the actual change");
        Click(dialog, "OkButton");

        bool fail = true;
        dialog = new SettingsWindow(original, new[] { "A", "B" }, "A", true, (settings, value) =>
        {
            if (fail) throw new System.IO.IOException("Cannot write settings");
            applied = settings; startup = value;
        });
        Element<ComboBox>(dialog, "ThemeChoice").SelectedIndex = (int)ThemeMode.Dark;
        check(Element<TextBlock>(dialog, "ErrorText").Text == "Cannot write settings", "Live apply failure remains visible in settings");
        fail = false;
        Click(dialog, "OkButton");
        check(applied?.Theme == ThemeMode.Dark, "OK retries a previously failed live change before accepting");
    }
}