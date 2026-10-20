using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
namespace YMusicTaskbar;

public partial class SettingsWindow : Window
{
    private readonly Settings draft;
    private readonly Func<Task> saveDiagnostics;
    private readonly Action<Settings, bool> save;
    private readonly string[] monitors;
    private string? selectedMonitor;
    private bool loadingProfile;
    public SettingsWindow(Settings settings, IEnumerable<string> monitors, string? primary, bool startup, Action<Settings, bool> save, Func<Task>? saveDiagnostics = null)
    {
        draft = JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(settings))!;
        this.saveDiagnostics = saveDiagnostics ?? (() => Task.CompletedTask);
        this.save = save; this.monitors = monitors.Distinct().ToArray();
        InitializeComponent();
        ThemeChoice.SelectedIndex = (int)draft.Theme;
        ThemeService.Apply(Resources, draft.Theme);
        ThemeChoice.SelectionChanged += (_, _) => ThemeService.Apply(Resources, (ThemeMode)ThemeChoice.SelectedIndex);
        PreferredChoice.Items.Add("Автоматически — основной экран");
        foreach (string name in this.monitors) PreferredChoice.Items.Add(name + (name == primary ? " (основной)" : ""));
        PreferredChoice.SelectedIndex = Math.Max(0, Array.IndexOf(this.monitors, draft.PreferredMonitor) + 1);
        StartupChoice.IsChecked = startup;
        foreach (string name in this.monitors) draft.Monitors[name] = draft.ProfileFor(name);
        MonitorSelector.ItemsSource = this.monitors.Select((id, index) => new MonitorTile(id, $"Монитор {index + 1}", id == primary ? "Основной" : id.Replace(@"\\.\", ""))).ToArray();
        MonitorSelector.SelectedIndex = Math.Max(0, Array.IndexOf(this.monitors, draft.PreferredMonitor ?? primary));
        ProfileEditor.IsEnabled = this.monitors.Length > 0;
        MaxHeight = Math.Max(MinHeight, SystemParameters.WorkArea.Height - 24);
        Height = Math.Min(Height, MaxHeight);
    }
    private void SelectMonitor(object sender, SelectionChangedEventArgs e)
    {
        if (MonitorSelector.SelectedItem is not MonitorTile tile) return;
        selectedMonitor = tile.Id; loadingProfile = true;
        var profile = draft.ProfileFor(tile.Id);
        EnabledChoice.IsChecked = profile.Enabled;
        SideChoice.SelectedIndex = (int)profile.Side;
        LayoutChoice.SelectedIndex = profile.Compact ? 1 : 0;
        System.Windows.Automation.AutomationProperties.SetName(SideChoice, "Выравнивание: " + tile.Id);
        System.Windows.Automation.AutomationProperties.SetName(LayoutChoice, "Вид: " + tile.Id);
        loadingProfile = false;
    }
    private void ProfileChanged(object sender, RoutedEventArgs e)
    {
        if (loadingProfile || selectedMonitor is null) return;
        var profile = draft.Monitors[selectedMonitor];
        profile.Enabled = EnabledChoice.IsChecked == true;
        profile.Side = (PlayerSide)Math.Max(0, SideChoice.SelectedIndex);
        profile.Compact = LayoutChoice.SelectedIndex == 1;
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
        if (monitors.Length > 0 && monitors.All(name => !draft.ProfileFor(name).Enabled)) { ErrorText.Text = "Выберите хотя бы один экран."; return; }
        draft.Theme = (ThemeMode)ThemeChoice.SelectedIndex;
        draft.PreferredMonitor = PreferredChoice.SelectedIndex > 0 ? monitors[PreferredChoice.SelectedIndex - 1] : null;
        try { save(draft, StartupChoice.IsChecked == true); Close(); }
        catch (Exception exception) { ErrorText.Text = exception.Message; }
    }
}