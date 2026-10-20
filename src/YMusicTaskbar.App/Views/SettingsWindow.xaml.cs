using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
namespace YMusicTaskbar;

public partial class SettingsWindow : Window
{
    private Settings draft;
    private readonly Settings original;
    private readonly bool originalStartup;
    private readonly Func<Task> saveDiagnostics;
    private readonly Action<Settings, bool> save;
    private readonly string[] monitors;
    private string? selectedMonitor;
    private bool loadingProfile, ready, accepted, changed;
    private string? appliedState;

    private static Settings Copy(Settings settings) => JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(settings))!;

    public SettingsWindow(Settings settings, IEnumerable<string> monitors, string? primary, bool startup, Action<Settings, bool> save, Func<Task>? saveDiagnostics = null)
    {
        original = Copy(settings); originalStartup = startup; draft = Copy(settings);
        this.saveDiagnostics = saveDiagnostics ?? (() => Task.CompletedTask);
        this.save = save; this.monitors = monitors.Distinct().ToArray();
        InitializeComponent();
        PreferredChoice.Items.Add("Автоматически — основной экран");
        foreach (string name in this.monitors) PreferredChoice.Items.Add(name + (name == primary ? " (основной)" : ""));
        foreach (string name in this.monitors) draft.Monitors[name] = draft.ProfileFor(name);
        MonitorSelector.ItemsSource = this.monitors.Select((id, index) => new MonitorTile(id, $"Монитор {index + 1}", id == primary ? "Основной" : id.Replace(@"\\.\", ""))).ToArray();
        MonitorSelector.SelectedIndex = Math.Max(0, Array.IndexOf(this.monitors, draft.PreferredMonitor ?? primary));
        LoadControls(startup);
        ProfileEditor.IsEnabled = this.monitors.Length > 0;
        MaxHeight = Math.Max(MinHeight, SystemParameters.WorkArea.Height - 24);
        Height = Math.Min(Height, MaxHeight);
        ThemeChoice.SelectionChanged += GeneralChanged;
        PreferredChoice.SelectionChanged += GeneralChanged;
        StartupChoice.Checked += GeneralChanged;
        StartupChoice.Unchecked += GeneralChanged;
        ready = true;
        appliedState = StateKey();
    }
    private string StateKey() => JsonSerializer.Serialize(draft) + "|" + (StartupChoice.IsChecked == true);

    private void LoadControls(bool startup)
    {
        loadingProfile = true;
        try
        {
            ThemeChoice.SelectedIndex = (int)draft.Theme;
            ThemeService.Apply(Resources, draft.Theme);
            PreferredChoice.SelectedIndex = Math.Max(0, Array.IndexOf(monitors, draft.PreferredMonitor) + 1);
            StartupChoice.IsChecked = startup;
            LoadProfile();
        }
        finally { loadingProfile = false; }
    }
    private void SelectMonitor(object sender, SelectionChangedEventArgs e) => LoadProfile();
    private void LoadProfile()
    {
        if (MonitorSelector.SelectedItem is not MonitorTile tile) return;
        bool wasLoading = loadingProfile;
        selectedMonitor = tile.Id; loadingProfile = true;
        try
        {
            var profile = draft.ProfileFor(tile.Id);
            EnabledChoice.IsChecked = profile.Enabled;
            SideChoice.SelectedIndex = (int)profile.Side;
            LayoutChoice.SelectedIndex = profile.Compact ? 1 : 0;
            System.Windows.Automation.AutomationProperties.SetName(SideChoice, "Выравнивание: " + tile.Id);
            System.Windows.Automation.AutomationProperties.SetName(LayoutChoice, "Вид: " + tile.Id);
        }
        finally { loadingProfile = wasLoading; }
    }
    private void ProfileChanged(object sender, RoutedEventArgs e)
    {
        if (!ready || loadingProfile || selectedMonitor is null) return;
        var profile = draft.Monitors[selectedMonitor];
        profile.Enabled = EnabledChoice.IsChecked == true;
        profile.Side = (PlayerSide)Math.Max(0, SideChoice.SelectedIndex);
        profile.Compact = LayoutChoice.SelectedIndex == 1;
        ApplyLive();
    }
    private void GeneralChanged(object sender, RoutedEventArgs e)
    {
        if (!ready || loadingProfile) return;
        ApplyLive();
    }
    private bool ApplyLive()
    {
        draft.Theme = (ThemeMode)Math.Max(0, ThemeChoice.SelectedIndex);
        draft.PreferredMonitor = PreferredChoice.SelectedIndex > 0 ? monitors[PreferredChoice.SelectedIndex - 1] : null;
        ThemeService.Apply(Resources, draft.Theme);
        if (monitors.Length > 0 && monitors.All(name => !draft.ProfileFor(name).Enabled))
        { ErrorText.Text = "Выберите хотя бы один экран."; return false; }
        string state = StateKey();
        try
        {
            if (state != appliedState)
            {
                // The runtime owns its copy; later edits must not change it before validation.
                changed = true;
                save(Copy(draft), StartupChoice.IsChecked == true);
                appliedState = state;
            }
            ErrorText.Text = "";
            return true;
        }
        catch (Exception exception) { appliedState = null; ErrorText.Text = exception.Message; return false; }
    }
    private void Reset(object sender, RoutedEventArgs e)
    {
        draft = new Settings();
        foreach (string monitor in monitors) draft.Monitors[monitor] = draft.ProfileFor(monitor);
        LoadControls(false);
        ApplyLive();
    }
    private async void SaveDiagnostics(object sender, RoutedEventArgs e)
    {
        DiagnosticsButton.IsEnabled = false;
        try { ErrorText.Text = ""; await saveDiagnostics(); }
        catch (Exception exception) { ErrorText.Text = exception.Message; }
        finally { DiagnosticsButton.IsEnabled = true; }
    }
    private void Cancel(object sender, RoutedEventArgs e) => Close();
    private void Save(object sender, RoutedEventArgs e)
    {
        if (!ApplyLive()) return;
        accepted = true;
        Close();
    }
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!accepted && changed)
        {
            try { save(Copy(original), originalStartup); changed = false; }
            catch (Exception exception) { ErrorText.Text = "Не удалось вернуть настройки: " + exception.Message; e.Cancel = true; }
        }
        base.OnClosing(e);
    }
}