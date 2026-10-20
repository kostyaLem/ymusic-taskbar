using YMusicTaskbar;

namespace YMusicTaskbar.AppTests;

internal sealed class MemoryStartup : IStartupRegistration
{
    public bool Enabled { get; private set; }
    public void SetEnabled(bool enabled) => Enabled = enabled;
}
