using System;
using System.IO;
using System.Windows;

namespace YMusicTaskbar;

public partial class App : Application
{
    private PlayerRuntime? runtime;
    private SingleInstanceLease? instance;
    protected override async void OnStartup(StartupEventArgs args)
    {
        base.OnStartup(args);
        DispatcherUnhandledException += (_, e) => { runtime?.Notifications.Report(e.Exception); e.Handled = true; };
        LaunchOptions? options = null;
        try
        {
            options = LaunchOptions.Parse(args.Args);
            if (options.Quit) { SingleInstanceLease.RequestExit(); Shutdown(); return; }
            var startup = new StartupRegistration(Environment.ProcessPath!);
            if (!options.Timed && !options.Diagnose) startup.RepairExistingRegistration();
            if (!options.Diagnose)
            {
                instance = new();
                if (!instance.Acquired && !options.Timed)
                {
                    await LegacyInstanceMigration.StopPrototypeAsync();
                    instance.Dispose(); instance = new();
                }
                if (!instance.Acquired) { Shutdown(); return; }
            }
            runtime = new(new MediaService(), new AudioService(), new DesktopService(), new JsonSettingsStore(),
                startup, new NotificationService(), options, instance, code => Shutdown(code));
            await runtime.StartAsync();
        }
        catch (Exception exception)
        {
            runtime?.Notifications.Report(exception);
            if (options is { } parsed && (parsed.Timed || parsed.Diagnose))
            { Directory.CreateDirectory(parsed.Output); File.WriteAllText(Path.Combine(parsed.Output, "error.txt"), exception.ToString()); }
            else MessageBox.Show(exception.Message, "Не удалось запустить панель", MessageBoxButton.OK, MessageBoxImage.Warning);
            Shutdown(1);
        }
    }
    protected override void OnExit(ExitEventArgs args)
    { runtime?.Dispose(); instance?.Dispose(); base.OnExit(args); }
}
