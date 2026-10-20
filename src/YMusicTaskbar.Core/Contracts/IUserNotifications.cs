using System;

namespace YMusicTaskbar;

public interface IUserNotifications
{
    void Notify(string message);
    void Report(Exception exception);
}
