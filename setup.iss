; Cursor Quota Progress Installer Script
; Inno Setup 6.x required

#define MyAppName "Cursor Quota Progress"
#ifndef MyAppVersion
#define MyAppVersion "1.0.0"
#endif
#define MyAppPublisher "Cursor Quota Progress"
#define MyAppExeName "CursorQuotaProgress.exe"
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
OutputBaseFilename=CursorQuotaProgress-{#MyAppVersion}-win-x64-setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile=Assets\cursor_quota_progress.ico

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
Type: filesandordirs; Name: "{localappdata}\CursorQuotaProgress"

[Code]
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;

  // Check if app is running
  if CheckForMutexes('CursorQuotaProgress_SingleInstance') then
  begin
    if MsgBox('Cursor Quota Progress is currently running. Please close it before continuing the installation.', mbError, MB_OKCANCEL) = IDOK then
    begin
      Result := False;
    end
    else
    begin
      Result := False;
    end;
  end;
end;

function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;

  // Check if app is running
  if CheckForMutexes('CursorQuotaProgress_SingleInstance') then
  begin
    if MsgBox('Cursor Quota Progress is currently running. Please close it before continuing the uninstallation.', mbError, MB_OKCANCEL) = IDOK then
    begin
      Result := False;
    end
    else
    begin
      Result := False;
    end;
  end;
end;
