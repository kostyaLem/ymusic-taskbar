using System;
using System.Threading.Tasks;
using System.Windows.Automation;
using YMusicTaskbar;

internal static class SeekBoundaryScenarios
{
    public static async Task RunAsync(IntegrationFixture test, RangeValuePattern slider)
    {
        using var media = new MediaService();
        await media.InitializeAsync(); await media.RefreshAsync();
        bool wasPlaying = media.Playing;
        double original = media.Position;
        string title = media.Title;
        double duration = media.Duration;
        try
        {
            if (wasPlaying) test.Check(await media.ExecuteAsync(PlayerCommand.TogglePlayPause), "Pause for deterministic boundary tests");
            await Task.Delay(400); await media.RefreshAsync();
            test.Check(!media.Playing, "Boundary tests start paused");
            await media.ExecuteAsync(PlayerCommand.Seek, duration / 2);
            await Task.Delay(600);
            await Task.Run(() => slider.SetValue(0));
            await Task.Delay(1100); await media.RefreshAsync();
            test.Check(media.Position > 0 && media.Position < 0.75, "UI zero seeks actual media to the beginning");
            test.Check(media.Title == title && !media.Playing, "Restart preserves track and paused state");
            await media.ExecuteAsync(PlayerCommand.Seek, duration / 2);
            await Task.Delay(600);
            await Task.Run(() => slider.SetValue(duration));
            await Task.Delay(1100); await media.RefreshAsync();
            test.Check(Math.Abs(media.Position - (duration - 0.5)) < 0.25, "UI end seeks actual media inside the final half second");
            test.Check(media.Title == title && !media.Playing, "End seek neither changes track nor starts playback");
        }
        finally
        {
            await media.ExecuteAsync(PlayerCommand.Seek, original);
            await Task.Delay(400); await media.RefreshAsync();
            if (media.Playing != wasPlaying) await media.ExecuteAsync(PlayerCommand.TogglePlayPause);
        }
    }
}
