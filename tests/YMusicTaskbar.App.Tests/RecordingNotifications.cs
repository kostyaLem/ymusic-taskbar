using YMusicTaskbar;

namespace YMusicTaskbar.AppTests;

internal sealed class RecordingNotifications : IUserNotifications
{
    public List<string> Messages { get; } = [];
    public void Notify(string message) => Messages.Add(message);
    public void Report(Exception exception) => Messages.Add(exception.Message);
}
