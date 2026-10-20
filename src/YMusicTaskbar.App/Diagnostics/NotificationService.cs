using System;
using System.IO;

namespace YMusicTaskbar;

public sealed class NotificationService : IUserNotifications
{
    private DateTimeOffset lastNotification;
    public string? LastError { get; private set; }
    public event Action<string>? Message;
    public void Notify(string message)
    {
        if (DateTimeOffset.UtcNow - lastNotification < TimeSpan.FromSeconds(10)) return;
        lastNotification = DateTimeOffset.UtcNow; Message?.Invoke(message);
    }
    public void Report(Exception exception) { Log(exception); Notify(exception.Message); }
    public void Log(Exception exception)
    {
        LastError = exception.Message;
        try
        {
            Directory.CreateDirectory(AppPaths.DirectoryPath); string path = Path.Combine(AppPaths.DirectoryPath, "errors.log");
            if (File.Exists(path) && new FileInfo(path).Length > 1024 * 1024) File.Move(path, path + ".old", true);
            File.AppendAllText(path, DateTimeOffset.Now + " " + exception + Environment.NewLine);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }
}
