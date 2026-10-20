using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace YMusicTaskbar;

public sealed class PlayerViewModel : INotifyPropertyChanged
{
    private readonly IMediaService media;
    private readonly IAudioService audio;
    private readonly IUserNotifications notifications;
    private bool busy, seeking;
    private string title = "Ожидание трека", artist = "";
    private byte[]? artwork;
    private bool playing, canSeek, audioAvailable, muted;
    private double position, duration = 1, volume;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? MenuRequested;
    public event EventHandler? OpenMusicRequested;
    public bool IsChangingVolume { get; set; }
    public string Title { get => title; private set => Set(ref title, value); }
    public string Artist { get => artist; private set => Set(ref artist, value); }
    public byte[]? Artwork { get => artwork; private set => Set(ref artwork, value); }
    public bool Playing { get => playing; private set => Set(ref playing, value); }
    public bool CanSeek { get => canSeek; private set => Set(ref canSeek, value); }
    public bool AudioAvailable { get => audioAvailable; private set => Set(ref audioAvailable, value); }
    public bool Muted { get => muted; private set => Set(ref muted, value); }
    public double Position { get => position; private set => Set(ref position, value); }
    public double Duration { get => duration; private set => Set(ref duration, value); }
    public double Volume { get => volume; private set => Set(ref volume, value); }
    public AsyncCommand Previous { get; }
    public AsyncCommand Next { get; }
    public AsyncCommand PlayPause { get; }
    public RelayCommand ToggleMute { get; }
    public RelayCommand ShowMenu { get; }
    public RelayCommand OpenMusic { get; }

    public PlayerViewModel(IMediaService media, IAudioService audio, IUserNotifications notifications)
    {
        this.media = media; this.audio = audio; this.notifications = notifications;
        Previous = new(() => ExecuteAsync(PlayerCommand.Previous), () => !busy && media.Capabilities.Previous, notifications.Report);
        Next = new(() => ExecuteAsync(PlayerCommand.Next), () => !busy && media.Capabilities.Next, notifications.Report);
        PlayPause = new(() => ExecuteAsync(PlayerCommand.TogglePlayPause), () => !busy &&
            (media.Capabilities.Toggle || (media.Playing ? media.Capabilities.Pause : media.Capabilities.Play)), notifications.Report);
        ToggleMute = new(() => MutateAudio(() => audio.SetMuted(!audio.Muted)), () => audio.Available);
        ShowMenu = new(() => MenuRequested?.Invoke(this, EventArgs.Empty));
        OpenMusic = new(() => OpenMusicRequested?.Invoke(this, EventArgs.Empty));
    }
    public void Refresh()
    {
        Title = media.Title; Artist = media.Artist; Artwork = media.Artwork; Playing = media.Playing;
        CanSeek = !busy && media.Capabilities.Seek && media.Duration > 0;
        Duration = Math.Max(1, media.Duration);
        if (!seeking && !busy) Position = media.DisplayPosition;
        RefreshAudio(); Previous.Refresh(); Next.Refresh(); PlayPause.Refresh();
    }
    public void RefreshAudio()
    {
        AudioAvailable = audio.Available; Muted = audio.Muted;
        if (!IsChangingVolume) Volume = audio.Level;
        ToggleMute.Refresh();
    }
    public void SetVolume(double level) => MutateAudio(() => { audio.SetLevel(level, unmute: true); Volume = audio.Level; });
    private void MutateAudio(Action action)
    {
        try { action(); RefreshAudio(); }
        catch (Exception exception) { notifications.Report(exception); }
    }
    public void BeginSeek() => seeking = true;
    public void PreviewSeek(double value) { if (seeking) Position = value; }
    public void EndSeek() => seeking = false;
    public Task SeekAsync(double value) => ExecuteAsync(PlayerCommand.Seek, value);
    public async Task ExecuteAsync(PlayerCommand command, double value = 0)
    {
        if (busy) return;
        busy = true; Refresh();
        try
        {
            if (!await media.ExecuteAsync(command, value)) notifications.Notify("Музыкальное приложение не выполнило команду.");
            await media.RefreshAsync();
        }
        catch (Exception exception) { notifications.Report(exception); }
        finally { busy = false; Refresh(); }
    }
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value; PropertyChanged?.Invoke(this, new(name));
    }
}
