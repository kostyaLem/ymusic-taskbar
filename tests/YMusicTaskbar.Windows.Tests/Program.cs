using System;
using System.Windows;

internal static class Program
{
    [STAThread]
    public static int Main(string[] arguments)
    {
        if (arguments.Length is < 2 or > 3) { Console.Error.WriteLine("Usage: tests <player.exe> <output-directory> [--menu-only|--placement-only]"); return 2; }
        NativeDpi.SetProcessDpiAwarenessContext(new IntPtr(-4));
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        application.Startup += async (_, _) =>
        {
            try
            {
                var mode = arguments.Length == 2 ? IntegrationMode.Full : arguments[2] switch
                {
                    "--menu-only" => IntegrationMode.MenuOnly,
                    "--placement-only" => IntegrationMode.PlacementOnly,
                    _ => throw new ArgumentException("Unknown integration mode")
                };
                using var fixture = new IntegrationFixture(arguments[0], arguments[1], mode);
                await fixture.RunAsync(); Console.WriteLine($"{fixture.Count} integration checks passed"); application.Shutdown(0);
            }
            catch (Exception exception) { Console.Error.WriteLine(exception); application.Shutdown(1); }
        };
        return application.Run();
    }
}
