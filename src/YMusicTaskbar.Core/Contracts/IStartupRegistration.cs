namespace YMusicTaskbar;

public interface IStartupRegistration
{
    bool Enabled { get; }
    void SetEnabled(bool enabled);
}
