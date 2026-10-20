using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace YMusicTaskbar;

internal sealed class PlayerRuntime : IDisposable
{
    private readonly IMediaService media;
    private readonly IAudioService audio;
    private readonly IDesktopService desktop;
    private readonly ISettingsStore store;
    private readonly IStartupRegistration startup;
    private readonly LaunchOptions options;
    private readonly SingleInstanceLease? instance;
    private readonly Action<int> shutdown;
    private readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
    private readonly DesktopMonitor monitors;
    private readonly Settings settings;
    private readonly PlayerViewModel viewModel;
    private PlayerWindow? window;
    private ScreenCaptureMonitor? captureMonitor;
    private PlayerTray? tray;
    private SettingsWindow? settingsWindow;
    private DispatcherTimer? timer;
    private bool refreshing, stopping, musicRunning;
    private DateTimeOffset nextProcess, nextAudio, nextTheme;
    private Placement? placement;
    private string status = "Ожидание музыкального приложения";
    public NotificationService Notifications { get; }

    public PlayerRuntime(IMediaService media, IAudioService audio, IDesktopService desktop, ISettingsStore store,
        IStartupRegistration startup, NotificationService notifications, LaunchOptions options,
        SingleInstanceLease? instance, Action<int> shutdown)
    {
        this.media = media; this.audio = audio; this.desktop = desktop; this.store = store; this.startup = startup;
        Notifications = notifications; this.options = options; this.instance = instance; this.shutdown = shutdown;
        settings = store.Load();
        if (options.Compact is { } compact)
        {
            settings.Compact = compact;
            foreach (var profile in settings.Monitors.Values) profile.Compact = compact;
        }
        viewModel = new(media, audio, notifications); monitors = new(desktop, notifications);
        audio.VolumeChanged += OnVolumeChanged; audio.SessionsInvalidated += OnSessionsInvalidated;
        viewModel.MenuRequested += (_, _) => tray?.ShowMenu();
        viewModel.OpenMusicRequested += (_, _) => desktop.OpenMusicWindow();
    }
    public async Task StartAsync()
    {
        await media.InitializeAsync();
        if (options.Diagnose)
        {
            await media.RefreshAsync(); audio.Refresh(); musicRunning = desktop.IsMusicRunning();
            var slots = await Task.Run(desktop.Inspect).WaitAsync(TimeSpan.FromSeconds(10));
            placement = PlacementPlanner.ChooseForSettings(slots, settings);
            await DiagnosticsWriter.SaveAsync(Path.Combine(options.Output, "diagnostics.json"), Capture() with { Monitors = slots });
            shutdown(0); return;
        }
        window = new(viewModel); window.SetCompact(settings.Compact);
        captureMonitor = new ScreenCaptureMonitor();
        captureMonitor.ForegroundChanged += (_, _) =>
        { if (!stopping && captureMonitor.IsSelecting) window.Hide(); };
        window.Closed += (_, _) => { if (!stopping) shutdown(0); };
        tray = new(settings, Notifications, OpenSettings, desktop.OpenMusicWindow, () => shutdown(0));
        Notifications.Message += tray.Notify;
        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        timer.Tick += async (_, _) => await RefreshAsync(); timer.Start(); await RefreshAsync();
        if (!options.Timed) return;
        await Task.Delay(options.TestHost ? 60000 : 7000);
        if (stopping) return;
        await DiagnosticsWriter.SaveAsync(Path.Combine(options.Output, "smoke.json"), Capture());
        if (window.IsVisible) window.SavePreview(Path.Combine(options.Output, "panel.png"));
        shutdown(0);
    }
    private void ApplySettings() { nextTheme = DateTimeOffset.MinValue; _ = RefreshAsync(); }
    private void OpenSettings()
    {
        if (settingsWindow is not null) { if (settingsWindow.WindowState == System.Windows.WindowState.Minimized) settingsWindow.WindowState = System.Windows.WindowState.Normal; settingsWindow.Activate(); return; }
        settingsWindow = new SettingsWindow(settings, System.Windows.Forms.Screen.AllScreens.Select(s => s.DeviceName),
            System.Windows.Forms.Screen.PrimaryScreen?.DeviceName, startup.Enabled, (draft, launch) =>
            {
                if (startup.Enabled != launch) startup.SetEnabled(launch);
                store.Save(draft);
                settings.Theme = draft.Theme; settings.PreferredMonitor = draft.PreferredMonitor;
                settings.Compact = draft.Compact; settings.Monitors = draft.Monitors;
                ApplySettings();
            }, SaveDiagnosticsAsync);
        settingsWindow.Closed += (_, _) => settingsWindow = null;
        settingsWindow.Show();
    }
    private async Task RefreshAsync()
    {
        if (refreshing || stopping) return;
        if (instance?.StopRequested == true) { shutdown(0); return; }
        refreshing = true;
        try
        {
            var now = DateTimeOffset.UtcNow;
            if (now >= nextProcess) { musicRunning = desktop.IsMusicRunning(); nextProcess = now.AddSeconds(1); }
            monitors.Tick(now); await media.RefreshAsync(); if (stopping) return;
            if (now >= nextAudio && window?.AudioInteracting != true)
            {
                try { audio.Refresh(); } catch (Exception exception) { audio.Reset(); Notifications.Log(exception); }
                nextAudio = now.AddSeconds(2);
            }
            viewModel.Refresh();
            if (window is not null && now >= nextTheme) { ThemeService.Apply(window.Resources, settings.Theme); nextTheme = now.AddSeconds(1); }
            // Snipping Tool already holds a frozen image of the player. Do not draw or move
            // the live overlay above it, including onto another monitor during selection.
            if (captureMonitor?.IsSelecting == true)
            {
                window?.Hide();
                status = "Выбор области снимка экрана";
            }
            else
            {
                placement = musicRunning ? PlacementPlanner.ChooseForSettings(monitors.Current, settings, window?.CompactWidth ?? 360) : null;
                if (placement is not null) { window?.SetCompact(settings.ProfileFor(placement.Monitor).Compact); window?.Place(placement); status = "Отображается"; }
                else { window?.Hide(); status = musicRunning ? "Скрыто: полноэкранный режим или нет места" : "Ожидание запуска музыкального приложения"; }
            }
            if (options.TestHost)
            {
                if (File.Exists(Path.Combine(options.Output, "stop.txt"))) { shutdown(0); return; }
                await DiagnosticsWriter.SaveAsync(Path.Combine(options.Output, "live.json"), Capture());
            }
        }
        catch (Exception exception) { window?.Hide(); Notifications.Report(exception); }
        finally { refreshing = false; }
    }
    private void OnVolumeChanged(object? sender, VolumeNotification change)
    {
        if (dispatcher.HasShutdownStarted) return;
        dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() =>
        { if (!stopping && audio.AcceptNotification(change)) viewModel.RefreshAudio(); }));
    }
    private void OnSessionsInvalidated(object? sender, EventArgs args)
    {
        if (!dispatcher.HasShutdownStarted) dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => nextAudio = DateTimeOffset.MinValue));
    }
    private RuntimeSnapshot Capture() => new(DateTimeOffset.Now, media.Snapshot(), audio.Snapshot(), musicRunning,
        window?.IsVisible ?? false, status, Notifications.LastError, placement, window?.UiState(), monitors.Current);
    private async Task SaveDiagnosticsAsync()
    {
        string path = Path.Combine(AppPaths.DirectoryPath, "diagnostics.json");
        await DiagnosticsWriter.SaveAsync(path, Capture());
        Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + path + "\"") { UseShellExecute = true });
    }
    public void Dispose()
    {
        if (stopping) return;
        stopping = true; timer?.Stop(); audio.VolumeChanged -= OnVolumeChanged; audio.SessionsInvalidated -= OnSessionsInvalidated;
        if (tray is not null) { Notifications.Message -= tray.Notify; tray.Dispose(); }
        captureMonitor?.Dispose(); settingsWindow?.Close(); media.Dispose(); audio.Dispose();
    }
}
