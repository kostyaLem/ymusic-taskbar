using YMusicTaskbar;

internal sealed class PreviewSettings : ISettingsStore, IStartupRegistration, IUserNotifications
{
    public Settings Load() => new();
    public void Save(Settings settings) => throw new InvalidOperationException("Documentation preview cannot save user settings");
    public bool Enabled => true;
    public void SetEnabled(bool enabled) => throw new InvalidOperationException("Documentation preview cannot change startup");
    public void Notify(string message) { }
    public void Report(Exception exception) => throw new InvalidOperationException("Preview failed", exception);
}
