using System.Collections.Generic;

namespace YMusicTaskbar;

public interface IDesktopService
{
    IReadOnlyList<DesktopSlot> Inspect();
    bool IsMusicRunning();
    void OpenMusicWindow();
}
