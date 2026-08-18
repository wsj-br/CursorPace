; Cursor Usage Progress Installer Script
; Inno Setup 6.x required

#define MyAppName "Cursor Usage Progress"
#ifndef MyAppVersion
#define MyAppVersion "1.0.0"
#endif
#define MyAppPublisher "Cursor Usage Progress"
#define MyAppExeName "CursorUsageProgress.exe"
#ifndef PublishDir
#define PublishDir "bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"
#endif

[Setup]
AppId={{B4F8A9C2-5E3D-4F1A-9B2C-8D7E6F4A5C3B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=installer
OutputBaseFilename=CursorUsageProgress-{#MyAppVersion}-win-x64-setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile=Assets\cursor_usage_progress.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\CursorUsageProgress"

[Code]
function WaitUntilAppClosed(const ActionPhrase: String): Boolean;
begin
  Result := True;
  while CheckForMutexes('CursorUsageProgress_SingleInstance') do
  begin
    if MsgBox('Cursor Usage Progress is currently running. Close it, then click Retry to continue ' + ActionPhrase + ', or click Cancel to abort.', mbError, MB_RETRYCANCEL) <> IDRETRY then
    begin
      Result := False;
      Exit;
    end;
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := WaitUntilAppClosed('the installation');
end;

function InitializeUninstall(): Boolean;
begin
  Result := WaitUntilAppClosed('the uninstallation');
end;
