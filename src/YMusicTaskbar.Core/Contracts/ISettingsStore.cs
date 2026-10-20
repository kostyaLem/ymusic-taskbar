namespace YMusicTaskbar;

public interface ISettingsStore
{
    Settings Load();
    void Save(Settings settings);
}
