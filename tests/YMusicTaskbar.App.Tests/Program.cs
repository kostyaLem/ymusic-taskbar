using YMusicTaskbar;
using System.IO;
using YMusicTaskbar.AppTests;

int count = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine("PASS " + name); count++; }
var media = new FakeMediaService(); var audio = new FakeAudioService(); var messages = new RecordingNotifications();
var model = new PlayerViewModel(media, audio, messages); model.Refresh();
Check(model.Title == media.Title && model.Artist == media.Artist, "Track metadata flows through view model");
model.ToggleMute.Execute(null);
Check(audio.Muted && model.Muted && audio.Level == 42, "Mute command preserves volume");
model.ToggleMute.Execute(null);
Check(!model.Muted && model.Volume == 42, "Unmute restores previous level");
model.ToggleMute.Execute(null); model.SetVolume(35);
Check(!model.Muted && audio.Level == 35 && model.Volume == 35, "Volume adjustment unmutes and updates model");
audio.Available = false; model.RefreshAudio();
Check(!model.AudioAvailable && !model.ToggleMute.CanExecute(null), "Unavailable audio disables mutation");
media.Capabilities = new(); model.Refresh();
Check(!model.Next.CanExecute(null) && !model.Previous.CanExecute(null) && !model.CanSeek, "Unsupported media controls disabled");
media.Capabilities = new(true, true, true, true, true, true); model.Refresh();
model.BeginSeek(); model.PreviewSeek(70); media.Position = 11; model.Refresh();
Check(model.Position == 70, "Background refresh preserves pointer seek preview");
model.EndSeek(); model.Refresh();
Check(model.Position == 11, "Canceled seek returns to real position");
await model.SeekAsync(60);
Check(media.LastCommand == PlayerCommand.Seek && media.LastValue == 60, "Seek forwards requested position");
media.Pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
var first = model.ExecuteAsync(PlayerCommand.Next); int calls = media.Calls;
await model.ExecuteAsync(PlayerCommand.Next);
Check(media.Calls == calls && !model.Next.CanExecute(null), "Busy command rejects overlapping invocation");
media.Pending.SetResult(true); await first; media.Pending = null;
Check(model.Next.CanExecute(null), "Controls recover after asynchronous command");
media.Accepted = false; await model.ExecuteAsync(PlayerCommand.Next);
Check(messages.Messages.Count == 1, "Rejected native command produces feedback");
int menu = 0; model.MenuRequested += (_, _) => menu++; model.ShowMenu.Execute(null);
Check(menu == 1, "Settings command is independent of Windows UI");
var parsed = LaunchOptions.Parse(["--compact", "--output", "artifacts"]);
Check(parsed.Compact == true && Path.IsPathFullyQualified(parsed.Output), "Command line resolves portable paths");
using var icon = typeof(PlayerViewModel).Assembly.GetManifestResourceStream("YMusicTaskbar.AppIcon");
Check(icon is not null && icon.Length > 1000, "Application contains custom icon resource");
string settingsPath = Path.Combine(AppContext.BaseDirectory, "settings-test-" + Guid.NewGuid() + ".json");
try
{
    var store = new JsonSettingsStore(settingsPath);
    Check(!store.Load().Compact, "Missing settings use defaults");
    store.Save(new Settings { Compact = true, PreferredMonitor = "monitor" });
    var loaded = store.Load();
    Check(loaded.Compact && loaded.PreferredMonitor == "monitor", "Settings store roundtrips user preferences");
    loaded.Theme = ThemeMode.Light;
    loaded.Monitors["monitor"] = new MonitorProfile { Compact = false, Side = PlayerSide.Right, Enabled = false };
    store.Save(loaded);
    Check(store.Load() is { Theme: ThemeMode.Light } persisted && persisted.Monitors["monitor"] is { Compact: false, Side: PlayerSide.Right, Enabled: false }, "Settings store persists themes and monitor profiles");
    File.WriteAllText(settingsPath, "invalid json");
    Check(!store.Load().Compact, "Invalid settings recover without crashing");
}
finally { File.Delete(settingsPath); File.Delete(settingsPath + ".tmp"); }
var system = WindowsVersionInfo.Capture();
Check(system.BuildNumber == Environment.OSVersion.Version.Build && system.Type is "Windows 10" or "Windows 11" or "Windows Server", "Diagnostics identify actual OS type and build");
var expectedRevision = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "UBR", null) as int?;
Check(system.Revision == expectedRevision && system.Revision is not null && system.Version.EndsWith($"{system.BuildNumber}.{system.Revision}"), "Diagnostics retain Windows update revision rather than only major version");
using (var systemJson = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(system)))
    Check(systemJson.RootElement.GetProperty("Revision").GetInt32() == expectedRevision && systemJson.RootElement.GetProperty("Architecture").GetString() is { Length: > 0 }, "Serialized diagnostics include OS revision and architecture");
MenuScenarios.Run(Check);
StartupScenarios.Run(Check);
SingleInstanceScenarios.Run(Check);
LayoutScenarios.Run(Check);
MediaIdentityScenarios.Run(Check);
FullscreenScenarios.Run(Check);
Console.WriteLine($"{count} application checks passed");
