#ifndef AppVersion
  #define AppVersion "0.3.1"
#endif
#define PreviousName "YandexMusicTaskbar"
#ifndef PublishDir
  #define PublishDir "..\publish\win-x64"
#endif

[Setup]
AppId={{8E24CD71-4DD7-4F8B-80B9-54EA4B20CB76}
AppName=YMusic Taskbar
AppVersion={#AppVersion}
AppPublisher=YMusicTaskbar contributors
DefaultDirName={localappdata}\Programs\YMusicTaskbar
DefaultGroupName=YMusic Taskbar
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19045
OutputDir=..\dist
OutputBaseFilename=YMusicTaskbar-{#AppVersion}-win-x64-Setup
SetupIconFile=..\assets\app.ico
UninstallDisplayIcon={app}\YMusicTaskbar.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
CloseApplications=yes
RestartApplications=no
Uninstallable=yes
VersionInfoDescription=YMusic Taskbar Setup
AppComments=Неофициальный мини-плеер для Яндекс Музыки
VersionInfoVersion={#AppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked
Name: "autostart"; Description: "{cm:AutoStart}"; Flags: unchecked

[CustomMessages]
english.AutoStart=Start with Windows
russian.AutoStart=Запускать вместе с Windows
english.LaunchPlayer=Launch YMusic Taskbar
russian.LaunchPlayer=Запустить YMusic Taskbar

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\YMusic Taskbar"; Filename: "{app}\YMusicTaskbar.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\YMusic Taskbar"; Filename: "{app}\YMusicTaskbar.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[InstallDelete]
Type: files; Name: "{autoprograms}\Yandex Music Taskbar.lnk"
Type: files; Name: "{autodesktop}\Yandex Music Taskbar.lnk"
Type: files; Name: "{app}\{#PreviousName}.exe"
Type: files; Name: "{app}\{#PreviousName}.dll"
Type: files; Name: "{app}\{#PreviousName}.deps.json"
Type: files; Name: "{app}\{#PreviousName}.runtimeconfig.json"
Type: files; Name: "{app}\{#PreviousName}.Core.dll"
Type: files; Name: "{app}\{#PreviousName}.Windows.dll"

[Run]
Filename: "{app}\YMusicTaskbar.exe"; Description: "{cm:LaunchPlayer}"; Flags: nowait postinstall skipifsilent

[Code]
const RunKey = 'Software\Microsoft\Windows\CurrentVersion\Run';

procedure StopInstalledPlayer();
var ResultCode: Integer; Executable: String;
begin
  Executable := ExpandConstant('{app}\YMusicTaskbar.exe');
  if FileExists(Executable) then begin
    Exec(Executable, '--quit', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(700);
  end;
  Executable := ExpandConstant('{app}\{#PreviousName}.exe');
  if FileExists(Executable) then begin
    Exec(Executable, '--quit', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(700);
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  StopInstalledPlayer();
  Result := '';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var PreviousCommand, CurrentCommand: String;
begin
  if (CurStep = ssPostInstall) and RegQueryStringValue(HKCU, RunKey, '{#PreviousName}', PreviousCommand) then
    if CompareText(ExtractFileName(RemoveQuotes(PreviousCommand)), '{#PreviousName}.exe') = 0 then begin
      if not RegQueryStringValue(HKCU, RunKey, 'YMusicTaskbar', CurrentCommand) then
        RegWriteStringValue(HKCU, RunKey, 'YMusicTaskbar', '"' + ExpandConstant('{app}\YMusicTaskbar.exe') + '"');
      RegDeleteValue(HKCU, RunKey, '{#PreviousName}');
    end;
  if (CurStep = ssPostInstall) and WizardIsTaskSelected('autostart') then
    RegWriteStringValue(HKCU, RunKey, 'YMusicTaskbar', '"' + ExpandConstant('{app}\YMusicTaskbar.exe') + '"');
end;

function InitializeUninstall(): Boolean;
begin
  StopInstalledPlayer();
  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var Value: String;
begin
  if (CurUninstallStep = usUninstall) and RegQueryStringValue(HKCU, RunKey, 'YMusicTaskbar', Value) then
    if CompareText(Value, '"' + ExpandConstant('{app}\YMusicTaskbar.exe') + '"') = 0 then
      RegDeleteValue(HKCU, RunKey, 'YMusicTaskbar');
end;
